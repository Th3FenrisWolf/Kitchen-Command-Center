import { execSync } from "node:child_process";
import { appendFileSync, closeSync, existsSync, fstatSync, mkdirSync, openSync, readFileSync, readSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { ask, findQuestion, loadBank, PLUGIN_BANK } from "./core.mjs";
import { decide } from "./decide.mjs";

// Owner steering. Each Claude Code hook event where a session waits on the
// owner runs one subcommand here. Jev decides whether the owner is needed;
// code floors and every unsure answer keep the current behaviour.

export const EVENTS = ["stop", "ask", "prompt", "session"];
const JEV_EVENTS = new Set(["stop", "ask", "prompt"]);
const MODES = new Set(["off", "shadow", "on"]);
const SESSION_TEXT = fileURLToPath(new URL("../templates/steer-session.md", import.meta.url));

export const DEFAULTS = {
  mode: { stop: "shadow", ask: "shadow", prompt: "on", session: "on" },
  jev: true,
  floors: [],
  neverHeaders: [],
  probes: [{ name: "GitHub", run: "gh auth status", fix: "gh auth login" }],
};

export const steerDir = () => process.env.BIZSTREAM_STEER_DIR ?? join(homedir(), ".claude", "steer");

function readJson(path) {
  if (!existsSync(path)) return {};
  try {
    return JSON.parse(readFileSync(path, "utf8"));
  } catch {
    return {};
  }
}

// Plugin defaults, then the user file, then the project file, then the
// environment. mode merges per event; every other key replaces. probes are
// shell commands, so a cloned repository never supplies them.
export function loadConfig({ env = process.env, projectDir = ".", home = homedir() } = {}) {
  const { probes: _untrusted, ...project } = readJson(join(projectDir, ".claude", "steer.json"));
  const layers = [readJson(join(home, ".claude", "steer.json")), project];
  const config = structuredClone(DEFAULTS);
  for (const layer of layers) {
    const { mode, ...rest } = layer;
    Object.assign(config, rest);
    Object.assign(config.mode, mode ?? {});
  }
  if (MODES.has(env.BIZSTREAM_STEER)) for (const event of EVENTS) config.mode[event] = env.BIZSTREAM_STEER;
  for (const event of EVENTS) {
    const value = env[`BIZSTREAM_STEER_${event.toUpperCase()}`];
    if (MODES.has(value)) config.mode[event] = value;
  }
  return config;
}

// A Jev steer with no key, or in a project that keeps its text local, is off:
// its floors alone would change nothing the session does today.
export function modeFor(event, config, env = process.env) {
  const mode = config.mode[event] ?? "off";
  if (JEV_EVENTS.has(event) && (!env.JEV_API_KEY || config.jev === false)) return "off";
  return MODES.has(mode) ? mode : "off";
}

export function redact(text) {
  return String(text ?? "")
    .replace(/-----BEGIN [A-Z ]+-----[\s\S]*?-----END [A-Z ]+-----/g, "[redacted]")
    .replace(/\b(?:sk|pk|rk|ghp|gho|ghs|ghu|github_pat|xox[abpr])[-_][A-Za-z0-9_-]{16,}/g, "[redacted]")
    .replace(/\bAKIA[0-9A-Z]{16}\b/g, "[redacted]")
    .replace(/\beyJ[\w-]{8,}\.[\w-]{8,}\.[\w-]{8,}/g, "[redacted]")
    .replace(/\b(password|pwd|secret|token|api[_-]?key|client[_-]?secret|connectionstring)(\s*[:=]\s*)("[^"]*"|'[^']*'|\S+)/gi, "$1$2[redacted]")
    .replace(/\b(?:Server|Data Source)=[^;\n]+;[^\n]*/gi, "[redacted connection string]")
    .replace(/[\w.+-]+@[\w-]+\.[\w.]+/g, "[email]");
}

// An invalid pattern in a config file is skipped, so one typo never disables
// the other floors.
const floorHit = (text, floors) =>
  floors.find((pattern) => {
    try {
      return new RegExp(pattern, "i").test(text);
    } catch {
      return false;
    }
  });

// The transcript can be many megabytes, so read only its tail.
export function lastAssistantText(path, bytes = 262144) {
  if (!path || !existsSync(path)) return "";
  const fd = openSync(path, "r");
  try {
    const size = fstatSync(fd).size;
    const length = Math.min(size, bytes);
    const buffer = Buffer.alloc(length);
    readSync(fd, buffer, 0, length, size - length);
    const lines = buffer.toString("utf8").split("\n").reverse();
    for (const line of lines) {
      let entry;
      try {
        entry = JSON.parse(line);
      } catch {
        continue;
      }
      if (entry.type !== "assistant" || entry.isSidechain) continue;
      const text = (entry.message?.content ?? []).filter((c) => c.type === "text").map((c) => c.text).join("\n");
      if (text.trim()) return text;
    }
    return "";
  } finally {
    closeSync(fd);
  }
}

export function askState(question) {
  return [
    `Header: ${question.header ?? ""}`,
    `Question: ${question.question}`,
    "Options:",
    ...(question.options ?? []).map((option) => `- ${option.label}: ${option.description ?? ""}`),
  ].join("\n");
}

const recommended = (question) => (question.options ?? []).find((option) => /\(Recommended\)/i.test(option.label));

export async function steerStop(input, { config, mode, transport, bank }) {
  if (input.stop_hook_active) return { row: null, output: null };
  const text = input.last_assistant_message ?? lastAssistantText(input.transcript_path);
  if (!text.trim()) return { row: null, output: null };
  const state = redact(text.slice(-1500));
  const floor = floorHit(state, config.floors);
  const result = floor
    ? { outcome: "stop", via: "floor", reason: floor }
    : await decide(state, findQuestion("steer-stopped-short", bank), { transport });
  const row = { event: "stop", mode, state, ...result };
  const output =
    mode === "on" && result.outcome === "continue"
      ? {
          decision: "block",
          reason:
            "Owner steering: open work remains that you can do alone. Continue it. End the turn only for an owner decision, an owner action, or a running job you watch in the background.",
        }
      : null;
  return { row, output };
}

export async function steerAsk(input, { config, mode, transport, bank }) {
  const questions = input.tool_input?.questions ?? [];
  const question = findQuestion("steer-owner-takes-default", bank);
  const predictions = [];
  for (const item of questions) {
    const pick = recommended(item);
    const state = redact(askState(item));
    let result;
    if (!pick) result = { outcome: "ask", via: "floor", reason: "no recommended option" };
    else if (config.neverHeaders.includes(item.header)) result = { outcome: "ask", via: "floor", reason: "never header" };
    else if (floorHit(state, config.floors)) result = { outcome: "ask", via: "floor", reason: floorHit(state, config.floors) };
    else result = await decide(state, question, { transport });
    predictions.push({ question: item.question, header: item.header, recommended: pick?.label, state, ...result });
  }
  const answerAll = predictions.length > 0 && predictions.every((p) => p.outcome === "answer");
  const row = { event: "ask", mode, tool_use_id: input.tool_use_id, predictions };
  if (mode !== "on" || !answerAll) return { row, output: null };
  const answers = Object.fromEntries(predictions.map((p) => [p.question, p.recommended]));
  return {
    row: { ...row, answered: true },
    output: {
      hookSpecificOutput: {
        hookEventName: "PreToolUse",
        permissionDecision: "allow",
        permissionDecisionReason: "Owner steering predicted the recommended options",
        updatedInput: { ...input.tool_input, answers },
      },
    },
  };
}

// PostToolUse on the same tool. It records what the owner chose, so each
// shadow prediction has its answer and each question becomes a labelled case.
export function steerAskResult(input) {
  const answers = input.tool_response?.answers ?? {};
  const results = (input.tool_input?.questions ?? []).map((item) => {
    const pick = recommended(item);
    const chosen = answers[item.question];
    return {
      question: item.question,
      state: redact(askState(item)),
      recommended: pick?.label,
      chosen: chosen === undefined ? null : redact(chosen),
      tookRecommended: Boolean(pick) && chosen === pick.label,
    };
  });
  return { row: { event: "ask-result", tool_use_id: input.tool_use_id, results }, output: null };
}

export async function steerPrompt(input, { mode, transport, bank }) {
  const prompt = String(input.prompt ?? "").trim();
  if (!prompt) return { row: null, output: null };
  const prior = redact(lastAssistantText(input.transcript_path).slice(-800));
  const state = `Agent's prior message (tail): ${prior || "(none)"}\n\nOwner's message: ${redact(prompt).slice(0, 1500)}`;
  const intent = await decide(state, findQuestion("owner-intent", bank), { transport });
  const [routine] = (await ask(state, [findQuestion("owner-routine", bank)], { transport })) ?? [];
  const row = { event: "prompt", mode, prompt: redact(prompt).slice(0, 300), intent: intent.outcome, routine: routine?.verdict ?? null };
  let output = null;
  if (intent.outcome === "correction") {
    const [fault] = (await ask(state, [findQuestion("owner-fault", bank)], { transport })) ?? [];
    row.fault = fault?.verdict ?? null;
    if (mode === "on") {
      output = {
        hookSpecificOutput: {
          hookEventName: "UserPromptSubmit",
          additionalContext:
            "Owner steering: the owner just corrected you. Fix it. Then, in this same turn, write the rule as a command where this project keeps its rules, so the correction is not needed again.",
        },
      };
    }
  }
  return { row, output };
}

export function steerSession(input, { config, run = defaultProbe }) {
  const failures = [];
  for (const probe of config.probes ?? []) {
    if (!run(probe.run)) failures.push(probe);
  }
  const lines = [readFileSync(SESSION_TEXT, "utf8").trim()];
  if (failures.length > 0) {
    lines.push("", "Preflight failures. Show the owner this list before any other work:");
    for (const probe of failures) lines.push(`- ${probe.name}: \`${probe.run}\` failed. Fix: \`${probe.fix}\``);
  }
  return {
    row: failures.length > 0 ? { event: "session", failures: failures.map((probe) => probe.name) } : null,
    output: { hookSpecificOutput: { hookEventName: "SessionStart", additionalContext: lines.join("\n") } },
  };
}

function defaultProbe(command) {
  try {
    execSync(command, { stdio: "ignore", timeout: 10000 });
    return true;
  } catch {
    return false;
  }
}

function log(row, input) {
  if (!row) return;
  mkdirSync(steerDir(), { recursive: true });
  const line = { ts: new Date().toISOString(), session: input.session_id, project: input.cwd, ...row };
  appendFileSync(join(steerDir(), "log.jsonl"), `${JSON.stringify(line)}\n`);
}

export function readLog(days = 7) {
  const path = join(steerDir(), "log.jsonl");
  if (!existsSync(path)) return [];
  const since = Date.now() - days * 86400000;
  return readFileSync(path, "utf8")
    .split("\n")
    .filter(Boolean)
    .map((line) => {
      try {
        return JSON.parse(line);
      } catch {
        return null;
      }
    })
    .filter((row) => row && Date.parse(row.ts) >= since);
}

// A shadow stop is judged by the owner's next prompt in the same session: a
// nudge or a status check means the agent stopped short.
const NUDGES = new Set(["proceed", "status"]);
const JUDGMENT = new Set(["decision", "signal", "directive", "observation"]);

// A question the steer answered never reached the owner, so its result only
// echoes the prediction and is no evidence either way.
const ownerResults = (rows) => {
  const answered = new Set(rows.filter((row) => row.event === "ask" && row.answered).map((row) => row.tool_use_id));
  return rows.filter((row) => row.event === "ask-result" && !answered.has(row.tool_use_id));
};

export function cases(rows) {
  const out = [];
  for (const row of ownerResults(rows)) {
    for (const result of row.results) {
      if (!result.recommended || result.chosen === null) continue;
      out.push({ question: "steer-owner-takes-default", expected: result.tookRecommended ? "pass" : "fail", state: result.state });
    }
  }
  const bySession = new Map();
  for (const row of rows) {
    if (!bySession.has(row.session)) bySession.set(row.session, []);
    bySession.get(row.session).push(row);
  }
  for (const list of bySession.values()) {
    list.forEach((row, index) => {
      if (row.event !== "stop" || row.via === "floor" || !row.state) return;
      const next = list.slice(index + 1).find((entry) => entry.event === "prompt");
      if (!next) return;
      if (NUDGES.has(next.intent)) out.push({ question: "steer-stopped-short", expected: "fail", state: row.state });
      else if (JUDGMENT.has(next.intent)) out.push({ question: "steer-stopped-short", expected: "pass", state: row.state });
    });
  }
  return out;
}

export function report(rows) {
  const count = (list, key) => list.reduce((acc, row) => ((acc[row[key]] = (acc[row[key]] ?? 0) + 1), acc), {});
  const prompts = rows.filter((row) => row.event === "prompt");
  const routine = prompts.filter((row) => row.routine === "fail").length;
  const results = ownerResults(rows).flatMap((row) => row.results);
  const predicted = new Map(
    rows.filter((row) => row.event === "ask").flatMap((row) => row.predictions.map((p) => [`${row.tool_use_id}|${p.question}`, p.outcome])),
  );
  let agree = 0;
  let wrongAnswer = 0;
  for (const row of ownerResults(rows)) {
    for (const result of row.results) {
      const outcome = predicted.get(`${row.tool_use_id}|${result.question}`);
      if (outcome === "answer" && result.tookRecommended) agree += 1;
      if (outcome === "answer" && !result.tookRecommended) wrongAnswer += 1;
    }
  }
  const lines = [
    `Owner messages: ${prompts.length}, routine: ${prompts.length ? Math.round((routine / prompts.length) * 100) : 0}%`,
    `Intents: ${JSON.stringify(count(prompts, "intent"))}`,
    `Corrections by fault: ${JSON.stringify(count(prompts.filter((row) => row.fault), "fault"))}`,
    `Stop steer outcomes: ${JSON.stringify(count(rows.filter((row) => row.event === "stop"), "outcome"))}`,
    `Asks answered by the owner: ${results.length}, took the recommended option: ${results.filter((r) => r.tookRecommended).length}`,
    `Asks answered by the steer: ${rows.filter((row) => row.event === "ask" && row.answered).length}`,
    `Ask steer, predicted "answer": ${agree} right, ${wrongAnswer} wrong`,
    `Labelled cases ready to export: ${cases(rows).length}`,
  ];
  return lines.join("\n");
}

async function main([command, ...rest]) {
  const days = (fallback) => (rest[0] && Number.isFinite(Number(rest[0])) ? Number(rest[0]) : fallback);
  if (command === "report") return console.log(report(readLog(days(7))));
  if (command === "export-cases") return console.log(cases(readLog(days(3650))).map((c) => JSON.stringify(c)).join("\n"));
  const handlers = { stop: steerStop, ask: steerAsk, "ask-result": steerAskResult, prompt: steerPrompt, session: steerSession };
  const handler = handlers[command];
  if (!handler) {
    console.error("usage: node steer.mjs <stop|ask|ask-result|prompt|session|report [days]|export-cases [days]>");
    return 2;
  }
  const input = JSON.parse(readFileSync(0, "utf8") || "{}");
  const projectDir = input.cwd ?? process.env.CLAUDE_PROJECT_DIR ?? ".";
  const config = loadConfig({ projectDir });
  const event = command === "ask-result" ? "ask" : command;
  const mode = modeFor(event, config);
  if (mode === "off") return 0;
  if (existsSync(projectDir)) process.chdir(projectDir);
  // The shipped bank only: a cloned repository never redefines when its owner is needed.
  const { row, output } = await handler(input, { config, mode, bank: loadBank([PLUGIN_BANK]) });
  log(row, input);
  if (output) console.log(JSON.stringify(output));
  return 0;
}

// A steer never breaks the session: any failure leaves the event as it was.
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    process.exitCode = (await main(process.argv.slice(2))) ?? 0;
  } catch (error) {
    try {
      log({ event: "error", message: String(error?.message ?? error).slice(0, 300) }, {});
    } catch {}
    process.exitCode = 0;
  }
}
