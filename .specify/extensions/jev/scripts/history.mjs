import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { basename, dirname, join } from "node:path";
import { pathToFileURL } from "node:url";
import { ask, FAIL, findQuestion, loadBank, PASS, PLUGIN_BANK, PLUGIN_CASES, REVIEW } from "./core.mjs";
import { cases, readLog, redact, steerDir } from "./steer.mjs";

// The history reader. It reads local session transcripts and the steer log,
// classifies every human turn through Jev, and writes a local baseline report.
// Every file it writes lives under historyDir(), never in a repository.

export const QUESTIONS = ["history-turn-class", "history-task-start", "history-task-autonomous"];
const CONCURRENCY = 8;
const USD_PER_TOKEN = 0.042 / 1e6;
const TURN_CONTEXT = 6000;
const TASK_LIMIT = 60000;
const SAMPLE = 30;
const AGREEMENT = 0.8;
const BAR = 20;
const JIRA_KEY = /\b[A-Z][A-Z0-9]+-\d+\b/g;
// Rows the harness writes as a user string that no person typed.
const NOT_HUMAN = /^\s*<(task-notification|local-command-stdout|local-command-stderr|local-command-caveat|bash-stdout|bash-stderr|system-reminder)>/;
const STOP_SEGMENTS = new Set(["users", "home", "documents", "projects", "src", ".claude", "worktrees", "_agents", "tmp", "jobs", "var", "private", "folders", "desktop", "repos", "code", "dev", "bizstream", "bizstream.ai"]);

export const historyDir = () => {
  const dir = join(steerDir(), "history");
  mkdirSync(dir, { recursive: true });
  return dir;
};
const file = (name) => join(historyDir(), name);
export const projectsRoot = () => process.env.BIZSTREAM_HISTORY_ROOT ?? join(homedir(), ".claude", "projects");

export function readJsonl(path) {
  if (!existsSync(path)) return [];
  return readFileSync(path, "utf8")
    .split("\n")
    .filter(Boolean)
    .flatMap((line) => {
      try {
        return [JSON.parse(line)];
      } catch {
        return [];
      }
    });
}
export const writeJsonl = (path, rows) => writeFileSync(path, rows.map((row) => `${JSON.stringify(row)}\n`).join(""));
const readJson = (path) => (existsSync(path) ? JSON.parse(readFileSync(path, "utf8")) : {});
const writeJson = (path, value) => writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`);

const textOf = (content) =>
  typeof content === "string" ? content : (content ?? []).filter((part) => part.type === "text").map((part) => part.text).join("\n");

export function parseSession(text) {
  const rows = [];
  let skipped = 0;
  for (const line of text.split("\n")) {
    if (!line.trim()) continue;
    try {
      rows.push(JSON.parse(line));
    } catch {
      skipped += 1;
    }
  }
  let sessionId = null;
  let assistant = [];
  const humanTurns = [];
  const asks = new Map();
  for (const row of rows) {
    sessionId ??= row.sessionId ?? null;
    if (row.type === "assistant") {
      for (const part of row.message?.content ?? []) {
        if (part.type === "text") assistant.push(part.text);
        if (part.type === "tool_use" && part.name === "AskUserQuestion") {
          asks.set(part.id, { id: part.id, ts: row.timestamp, cwd: row.cwd, questions: part.input?.questions ?? [] });
        }
      }
      continue;
    }
    if (row.type !== "user") continue;
    const content = row.message?.content;
    if (Array.isArray(content) && content.some((part) => part.type === "tool_result")) {
      for (const part of content) {
        const pending = part.type === "tool_result" ? asks.get(part.tool_use_id) : undefined;
        if (pending && row.toolUseResult?.answers) pending.answers = row.toolUseResult.answers;
      }
      continue;
    }
    // A string, or text parts beside an image, is something a person typed.
    const prompt = textOf(content);
    const human = prompt.trim() !== "" && (row.origin?.kind ?? "human") === "human" && !row.isMeta && !row.toolUseResult && !NOT_HUMAN.test(prompt);
    if (!human) continue;
    humanTurns.push({ uuid: row.uuid, ts: row.timestamp, cwd: row.cwd, branch: row.gitBranch, prompt, before: assistant.join("\n") });
    assistant = [];
  }
  return { sessionId, rows: rows.length, humanTurns, after: assistant.join("\n"), asks: [...asks.values()].filter((a) => a.answers), skipped };
}

export function listTranscripts(root = projectsRoot()) {
  if (!existsSync(root)) return [];
  return readdirSync(root, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .flatMap((entry) =>
      readdirSync(join(root, entry.name), { withFileTypes: true })
        .filter((child) => child.isFile() && child.name.endsWith(".jsonl"))
        .map((child) => join(root, entry.name, child.name)),
    );
}

// A turn's state is what the person read before they typed, plus what they typed.
export const turnState = (turn) => redact(`Assistant before:\n${turn.before.slice(-TURN_CONTEXT)}\n\nHuman:\n${turn.prompt}`);

export function taskState(turns, after) {
  const text = `${turns.map((turn, index) => `Human ${index + 1}:\n${turn.prompt}`).join("\n\n")}\n\nAssistant at the end:\n${after}`;
  if (text.length <= TASK_LIMIT) return redact(text);
  const half = TASK_LIMIT / 2;
  return redact(`${text.slice(0, half)}\n[...]\n${text.slice(-half)}`);
}

export const normalizeRemote = (url) =>
  url
    .trim()
    .replace(/^[a-z+]+:\/\//i, "")
    .replace(/^[^@/]+@/, "")
    .replace(/^([^/:]+):(?!\d+\/)/, "$1/")
    .replace(/\.git$/, "")
    .replace(/\/+$/, "")
    .toLowerCase();

// Worktrees of one repository share a remote and a common git dir, so they
// count as one repository. A cwd that no longer exists cannot be traced back,
// so it is unresolved and never counts toward the generic rule.
const repoCache = new Map();
export function repoKey(cwd) {
  if (!cwd) return "unresolved:";
  if (repoCache.has(cwd)) return repoCache.get(cwd);
  let key = null;
  if (existsSync(cwd)) {
    const git = (...args) => {
      try {
        return execFileSync("git", ["-C", cwd, ...args], { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] }).trim();
      } catch {
        return "";
      }
    };
    const remote = git("remote", "get-url", "origin");
    const common = remote ? "" : git("rev-parse", "--path-format=absolute", "--git-common-dir");
    if (remote) key = normalizeRemote(remote);
    else if (common) key = dirname(common).replace(/\\/g, "/");
  }
  if (!key) {
    const path = cwd.replace(/\\/g, "/");
    const stripped = path.replace(/\/\.claude\/worktrees\/[^/]+.*$/, "");
    key = existsSync(cwd) ? stripped : `unresolved:${stripped}`;
  }
  repoCache.set(cwd, key);
  return key;
}

// ask() turns a split sample into a review, but a one-sample choice needs its
// confidence floor applied here, as decide.mjs does.
export const judged = ([result], question) =>
  question.primitive === "choice" && result.confidence < (question.minConfidence ?? 0) ? REVIEW : result.verdict;

// Run at most `max` calls at once.
function limiter(max) {
  let active = 0;
  const queue = [];
  const next = () => {
    if (active >= max || queue.length === 0) return;
    active += 1;
    const { fn, resolve, reject } = queue.shift();
    fn()
      .then(resolve, reject)
      .finally(() => {
        active -= 1;
        next();
      });
  };
  return (fn) => new Promise((resolve, reject) => (queue.push({ fn, resolve, reject }), next()));
}

const projectOf = (path) => basename(dirname(path));

function pending(root, only) {
  const index = readJson(file("index.json"));
  return listTranscripts(root)
    .filter((path) => !only || only.includes(projectOf(path)))
    .flatMap((path) => {
      const stat = statSync(path);
      const seen = index[path];
      return seen && seen.size === stat.size && seen.mtime === stat.mtimeMs ? [] : [{ path, size: stat.size, mtime: stat.mtimeMs }];
    });
}

export function scan({ root = projectsRoot(), only } = {}) {
  let turns = 0;
  let chars = 0;
  const todo = pending(root, only);
  const projects = new Map();
  for (const entry of todo) {
    const session = parseSession(readFileSync(entry.path, "utf8"));
    turns += session.humanTurns.length;
    const project = projects.get(projectOf(entry.path)) ?? { dir: projectOf(entry.path), sessions: 0, turns: 0 };
    project.sessions += 1;
    project.turns += session.humanTurns.length;
    projects.set(project.dir, project);
    for (const turn of session.humanTurns) chars += turnState(turn).length;
    // ponytail: one task state per session; tasks are unknown until classify.
    if (session.humanTurns.length) chars += taskState(session.humanTurns, session.after).length;
  }
  const tokensEstimate = Math.ceil(chars / 4);
  return {
    sessions: todo.length,
    turns,
    projects: [...projects.values()].sort((a, b) => b.turns - a.turns),
    tokensEstimate,
    costEstimateUsd: Number((tokensEstimate * USD_PER_TOKEN).toFixed(4)),
    costNote: "lower bound: counts one pass over each turn, not the probe request, the second question per turn or the task questions",
  };
}

// A row belongs to a transcript file; a row written before files were recorded belongs to its session.
const replaceSession = (name, path, session, rows) =>
  writeJsonl(file(name), [...readJsonl(file(name)).filter((row) => (row.file ? row.file !== path : row.session !== session)), ...rows]);

export function cardsIn(turns) {
  const cards = new Set();
  for (const turn of turns) {
    if (turn.branch && !["main", "master", "HEAD"].includes(turn.branch)) cards.add(`branch:${turn.branch}`);
    for (const key of `${turn.before}\n${turn.prompt}`.match(JIRA_KEY) ?? []) cards.add(key);
  }
  return cards.size;
}

async function classifySession(entry, questions, call, opts) {
  const session = parseSession(readFileSync(entry.path, "utf8"));
  const id = session.sessionId ?? entry.path;
  const [classQ, startQ, autoQ] = questions;
  const verdicts = await Promise.all(session.humanTurns.map((turn) => call(() => ask(turnState(turn), [classQ, startQ], opts))));
  const turns = [];
  const tasks = [];
  session.humanTurns.forEach((turn, index) => {
    const [cls, start] = verdicts[index];
    const clsVerdict = judged([cls], classQ);
    const starts = index === 0 || start.verdict === PASS;
    if (starts) tasks.push({ id: `${id}:${tasks.length + 1}`, session: id, file: entry.path, cwd: turn.cwd, ts: turn.ts, members: [] });
    const task = tasks.at(-1);
    task.members.push(index);
    turns.push({
      id: `${id}:${turn.uuid}`,
      session: id,
      file: entry.path,
      ts: turn.ts,
      cwd: turn.cwd,
      branch: turn.branch ?? null,
      repo: repoKey(turn.cwd),
      task: task.id,
      first: starts,
      taskStart: start.verdict,
      class: clsVerdict,
      sampled: null,
      owner: null,
    });
  });
  const autonomy = await Promise.all(
    tasks.map((task, n) => {
      const members = task.members.map((index) => session.humanTurns[index]);
      const after = n === tasks.length - 1 ? session.after : session.humanTurns[tasks[n + 1].members[0]].before;
      return call(() => ask(taskState(members, after), [autoQ], opts));
    }),
  );
  const misreads = [];
  for (const task of tasks) {
    task.members.forEach((index, position) => {
      if (turns[index].class !== "misread") return;
      const span = task.members.slice(0, position + 1).map((i) => session.humanTurns[i]);
      misreads.push({ turn: turns[index].id, session: id, file: entry.path, turnsTouched: span.length, cardsTouched: cardsIn(span) });
    });
  }
  return {
    id,
    turns,
    tasks: tasks.map(({ members, ...task }, n) => ({
      ...task,
      firstTurn: turns[members[0]].id,
      turns: members.length,
      autonomous: autonomy[n][0].verdict,
      owner: null,
    })),
    misreads,
  };
}

// Parsed sessions held in memory at once.
const SESSION_POOL = 4;

export async function classify({ root = projectsRoot(), transport, only } = {}) {
  if (!process.env.JEV_API_KEY) throw new Error("JEV_API_KEY is not set: nothing classified");
  const bank = loadBank([PLUGIN_BANK]);
  const questions = QUESTIONS.map((id) => findQuestion(id, bank));
  const call = limiter(CONCURRENCY);
  const opts = { required: true, ...(transport ? { transport } : {}) };
  const todo = pending(root, only);
  // One request first: a bad key or an unreachable Jev stops the run before anything is written.
  for (const entry of todo) {
    const [first] = parseSession(readFileSync(entry.path, "utf8")).humanTurns;
    if (!first) continue;
    await call(() => ask(turnState(first), [questions[0]], opts));
    break;
  }
  const index = readJson(file("index.json"));
  const total = { turns: 0, tasks: 0, review: 0, errors: 0 };
  // Each session is written as it finishes, so an interrupted run keeps what it paid for.
  const persist = (entry, result) => {
    // A grown session is classified again; the owner's answers on it stay.
    const oldTurns = new Map(readJsonl(file("turns.jsonl")).map((turn) => [turn.id, turn]));
    const oldTasks = new Map(readJsonl(file("tasks.jsonl")).map((task) => [task.firstTurn, task]));
    for (const turn of result.turns) Object.assign(turn, { sampled: oldTurns.get(turn.id)?.sampled ?? null, owner: oldTurns.get(turn.id)?.owner ?? null });
    for (const task of result.tasks) task.owner = oldTasks.get(task.firstTurn)?.owner ?? null;
    replaceSession("turns.jsonl", entry.path, result.id, result.turns);
    replaceSession("tasks.jsonl", entry.path, result.id, result.tasks);
    replaceSession("misreads.jsonl", entry.path, result.id, result.misreads);
    index[entry.path] = { size: entry.size, mtime: entry.mtime, turns: result.turns.length };
    writeJson(file("index.json"), index);
    total.turns += result.turns.length;
    total.tasks += result.tasks.length;
    total.review += result.turns.filter((turn) => turn.class === REVIEW || turn.taskStart === REVIEW).length;
  };
  const queue = [...todo];
  const worker = async () => {
    for (let entry = queue.shift(); entry; entry = queue.shift()) {
      try {
        persist(entry, await classifySession(entry, questions, call, opts));
      } catch {
        total.errors += 1; // left out of the index, so the next run reads it again
      }
    }
  };
  await Promise.all(Array.from({ length: Math.min(SESSION_POOL, todo.length) }, worker));
  return total;
}

// A transcript Claude Code has since deleted, or a turn no longer in it, has no state.
function sessionOf(turn) {
  if (!existsSync(turn.file)) return null;
  const session = parseSession(readFileSync(turn.file, "utf8"));
  const turnIndex = session.humanTurns.findIndex((entry) => `${turn.session}:${entry.uuid}` === turn.id);
  return turnIndex < 0 ? null : { session, turnIndex };
}

function stateFor(turn, turns) {
  const found = sessionOf(turn);
  if (!found) {
    process.stderr.write(`skipped ${turn.id}: its transcript or turn is gone\n`);
    return null;
  }
  const { session, turnIndex } = found;
  const members = turns.filter((entry) => entry.task === turn.task);
  const indexes = members.map((member) => session.humanTurns.findIndex((entry) => `${turn.session}:${entry.uuid}` === member.id)).filter((i) => i >= 0);
  const last = Math.max(...indexes);
  const after = last === session.humanTurns.length - 1 ? session.after : session.humanTurns[last + 1].before;
  return { turn: turnState(session.humanTurns[turnIndex]), task: taskState(indexes.map((i) => session.humanTurns[i]), after) };
}

export function sample(n = SAMPLE) {
  const turns = readJsonl(file("turns.jsonl"));
  const tasks = new Map(readJsonl(file("tasks.jsonl")).map((task) => [task.id, task]));
  const unsure = (turn) => turn.class === REVIEW || turn.taskStart === REVIEW || (turn.first && tasks.get(turn.task)?.autonomous === REVIEW);
  for (const turn of turns) if (!turn.sampled && unsure(turn)) turn.sampled = "review";
  const states = new Map();
  const stateOf = (turn) => (states.has(turn.id) ? states.get(turn.id) : states.set(turn.id, stateFor(turn, turns)).get(turn.id));
  const random = turns.filter((turn) => turn.sampled === "random");
  const pool = turns.filter((turn) => !turn.sampled && turn.class !== REVIEW);
  while (random.length < n && pool.length > 0) {
    const [pick] = pool.splice(Math.floor(Math.random() * pool.length), 1);
    if (!stateOf(pick)) continue;
    pick.sampled = "random";
    random.push(pick);
  }
  writeJsonl(file("turns.jsonl"), turns);
  const items = turns
    .filter((turn) => turn.sampled && !turn.owner)
    .flatMap((turn) => {
      const state = stateOf(turn);
      if (!state) return [];
      const item = { id: turn.id, sampled: turn.sampled, class: turn.class, taskStart: turn.taskStart, state: state.turn };
      if (turn.first) Object.assign(item, { autonomous: tasks.get(turn.task)?.autonomous, taskState: state.task });
      return [item];
    });
  return { items };
}

export function agreementOf(turns) {
  const answered = turns.filter((turn) => turn.sampled === "random" && turn.owner);
  const agreed = answered.filter((turn) => turn.owner.class === turn.class).length;
  return { agreement: answered.length ? agreed / answered.length : 0, randomSampled: answered.length };
}

export function answer(turnId, cls, taskStart, autonomous) {
  const usage = "usage: answer <id> <class> <taskStart> [autonomous]; autonomous is required on a task's first turn";
  const classes = Object.keys(findQuestion("history-turn-class", loadBank([PLUGIN_BANK])).criteria);
  if (!classes.includes(cls)) throw new Error(`class must be one of ${classes.join(", ")}`);
  for (const value of [taskStart, autonomous].filter((v) => v !== undefined)) {
    if (![PASS, FAIL].includes(value)) throw new Error("taskStart and autonomous are pass or fail");
  }
  if (taskStart === undefined) throw new Error(usage);
  const turns = readJsonl(file("turns.jsonl"));
  const turn = turns.find((entry) => entry.id === turnId);
  if (!turn) throw new Error(`no turn ${turnId}`);
  if (turn.owner) throw new Error(`turn ${turnId} is already answered`);
  if (!turn.first && autonomous !== undefined) throw new Error("autonomous applies to a task's first turn only");
  if (turn.first && autonomous === undefined) throw new Error(usage);
  const state = stateFor(turn, turns);
  if (!state) return agreementOf(turns);
  turn.owner = { class: cls, taskStart };
  const rows = [
    { question: "history-turn-class", expected: cls, state: state.turn },
    { question: "history-task-start", expected: taskStart, state: state.turn },
  ];
  if (autonomous !== undefined) {
    const tasks = readJsonl(file("tasks.jsonl"));
    tasks.find((task) => task.id === turn.task).owner = autonomous;
    writeJsonl(file("tasks.jsonl"), tasks);
    rows.push({ question: "history-task-autonomous", expected: autonomous, state: state.task });
  }
  writeJsonl(file("turns.jsonl"), turns);
  writeJsonl(file("cases.jsonl"), [...readJsonl(file("cases.jsonl")), ...rows]);
  return agreementOf(turns);
}

export async function check({ transport } = {}) {
  if (!process.env.JEV_API_KEY) throw new Error("JEV_API_KEY is not set: nothing checked");
  const bank = loadBank([PLUGIN_BANK]);
  const all = [...readJsonl(PLUGIN_CASES), ...readJsonl(file("cases.jsonl"))].filter((row) => QUESTIONS.includes(row.question));
  const limit = limiter(CONCURRENCY);
  const questions = {};
  for (const id of QUESTIONS) {
    const question = findQuestion(id, bank);
    const rows = all.filter((row) => row.question === id);
    const verdicts = await Promise.all(rows.map((row) => limit(() => ask(row.state, [question], { required: true, ...(transport ? { transport } : {}) }))));
    const misses = verdicts.filter((answers, i) => {
      const verdict = judged(answers, question);
      return verdict !== REVIEW && verdict !== rows[i].expected;
    }).length;
    questions[id] = { cases: rows.length, misses, pass: rows.length >= BAR && misses === 0 };
  }
  writeJson(file("calibration.json"), questions);
  return { pass: Object.values(questions).every((q) => q.pass), questions };
}

const median = (values) => {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
};
const pct = (part, whole) => (whole ? `${Math.round((part / whole) * 1000) / 10}%` : "n/a");

export function gate(turns, misreads) {
  const decided = turns.filter((turn) => turn.class !== REVIEW).length;
  const share = decided ? turns.filter((turn) => turn.class === "misread").length / decided : 0;
  const cards = median(misreads.map((row) => row.cardsTouched));
  const signals = [share >= 0.1 && "misread share", cards > 1 && "median cards"].filter(Boolean);
  return { state: signals.length ? "open" : "closed", misreadShare: share, medianCards: cards, signals };
}

export function report() {
  const turns = readJsonl(file("turns.jsonl"));
  const tasks = readJsonl(file("tasks.jsonl"));
  const misreads = readJsonl(file("misreads.jsonl"));
  const calibration = readJson(file("calibration.json"));
  const { agreement, randomSampled } = agreementOf(turns);
  const calibrated = QUESTIONS.every((id) => calibration[id]?.pass);
  const provisional = agreement < AGREEMENT || randomSampled < SAMPLE || !calibrated;
  const decidedTasks = tasks.filter((task) => task.autonomous !== REVIEW);
  const g = gate(turns, misreads);
  const classes = ["lookup", "decision", "misread", "late-steer", "other", REVIEW];
  const worstTasks = [...tasks].sort((a, b) => b.turns - a.turns).slice(0, 10);
  const lines = [
    `# History baseline${provisional ? " (PROVISIONAL)" : ""}`,
    "",
    `Run ${new Date().toISOString().slice(0, 10)}. Sessions ${new Set(turns.map((t) => t.session)).size}, human turns ${turns.length}, tasks ${tasks.length}.`,
    "",
    "## Autonomy",
    "",
    `Tasks reached with no further human turn: ${pct(decidedTasks.filter((t) => t.autonomous === PASS).length, decidedTasks.length)} of ${decidedTasks.length} decided tasks.`,
    "",
    "## Turn classes",
    "",
    "| Class | Turns | Share |",
    "|---|---|---|",
    ...classes.map((cls) => {
      const count = turns.filter((t) => t.class === cls).length;
      return `| ${cls === REVIEW ? "unclassified (review)" : cls} | ${count} | ${pct(count, turns.length)} |`;
    }),
    "",
    "## Misread cost",
    "",
    `Misreads ${misreads.length}. Turns touched: median ${median(misreads.map((m) => m.turnsTouched))}, max ${Math.max(0, ...misreads.map((m) => m.turnsTouched))}. Cards touched: median ${median(misreads.map((m) => m.cardsTouched))}, max ${Math.max(0, ...misreads.map((m) => m.cardsTouched))}. A misread never corrected is not counted, so these are lower bounds.`,
    "",
    "## Owner check",
    "",
    `Agreement ${pct(agreement * randomSampled, randomSampled)} over ${randomSampled} random turns. Calibration: ${QUESTIONS.map((id) => `${id} ${calibration[id] ? `${calibration[id].cases} cases, ${calibration[id].misses} misses` : "not checked"}`).join("; ")}.`,
    "",
    "## Option B gate",
    "",
    `${g.state.toUpperCase()}. Misread share ${pct(g.misreadShare, 1)} (opens at 10%). Median cards per misread ${g.medianCards} (opens above 1).${g.signals.length ? ` Opened by: ${g.signals.join(", ")}.` : ""}`,
    "",
    "## Longest tasks",
    "",
    "| Session | Date | Human turns |",
    "|---|---|---|",
    ...worstTasks.map((task) => `| ${task.session} | ${String(task.ts ?? "").slice(0, 10)} | ${task.turns} |`),
    "",
  ];
  writeFileSync(file("report.md"), lines.join("\n"));
  return { path: file("report.md"), provisional, gate: g.state };
}

const hash = (value) => createHash("sha256").update(value).digest("hex").slice(0, 16);

export function points({ steerRows = readLog(3650) } = {}) {
  const answered = new Set(steerRows.filter((row) => row.event === "ask" && row.answered).map((row) => row.tool_use_id));
  const rejected = new Set(readJsonl(file("candidates.jsonl")).filter((c) => c.state === "rejected").flatMap((c) => c.points ?? []));
  const out = [];
  for (const path of Object.keys(readJson(file("index.json")))) {
    if (!existsSync(path)) continue;
    for (const item of parseSession(readFileSync(path, "utf8")).asks) {
      if (answered.has(item.id)) continue;
      for (const q of item.questions) {
        const labels = (q.options ?? []).map((option) => option.label);
        out.push({
          id: `${item.id}:${hash(q.question ?? "")}`,
          kind: "ask",
          cwd: item.cwd,
          repo: repoKey(item.cwd),
          question: redact(q.question),
          options: labels.map(redact),
          recommended: labels.find((label) => /\(Recommended\)/.test(label)) ?? null,
          chosen: redact(item.answers[q.question] ?? ""),
        });
      }
    }
  }
  for (const row of cases(steerRows)) out.push({ id: `steer:${hash(row.state)}`, kind: "steer-case", cwd: null, repo: null, ...row });
  for (const turn of readJsonl(file("turns.jsonl")).filter((t) => t.class === "misread")) {
    const state = stateFor(turn, readJsonl(file("turns.jsonl")));
    if (state) out.push({ id: turn.id, kind: "misread", cwd: turn.cwd, repo: turn.repo, state: state.turn });
  }
  return { points: out.filter((point) => !rejected.has(point.id)) };
}

const TRANSITIONS = { proposed: ["approved", "rejected"], approved: ["calibrated", "failed"], failed: ["approved"], calibrated: ["banked"] };

export function candidate(action, id, state, reason, body = {}) {
  const rows = readJsonl(file("candidates.jsonl"));
  if (action === "list") return { candidates: rows };
  if (action !== "set") throw new Error("usage: candidate set <id> <state> [reason] | candidate list");
  const existing = rows.find((row) => row.id === id);
  if (!existing) {
    if (state !== "proposed") throw new Error(`no candidate ${id}; create it as proposed`);
    rows.push({ id, scope: body.scope, repo: body.repo ?? null, question: body.question, rows: body.rows ?? [], points: body.points ?? [], state, reason: null });
  } else {
    if (!(TRANSITIONS[existing.state] ?? []).includes(state)) throw new Error(`${id}: ${existing.state} cannot move to ${state}`);
    Object.assign(existing, { state, reason: reason ?? null });
  }
  writeJsonl(file("candidates.jsonl"), rows);
  return { candidates: rows };
}

export function blockList() {
  const terms = new Map();
  for (const turn of readJsonl(file("turns.jsonl"))) {
    for (const segment of String(turn.cwd ?? "").split(/[\\/]/)) {
      if (segment.length >= 3 && !STOP_SEGMENTS.has(segment.toLowerCase())) terms.set(segment.toLowerCase(), "path");
    }
    const repo = String(turn.repo ?? "");
    if (!repo.startsWith("unresolved:") && !repo.startsWith("/")) {
      for (const part of repo.split("/").slice(1)) {
        if (part.length >= 3 && !STOP_SEGMENTS.has(part)) terms.set(part, terms.get(part) ?? "remote");
      }
    }
  }
  return terms;
}

const escape = (term) => term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
const URL_RE = /\bhttps?:\/\/[^\s"\\]+|\bwww\.[^\s"\\]+/i;
// ponytail: a host is a name under a common TLD; a rarer TLD slips through unless it is a URL.
const HOST_RE = /\b[a-z0-9-]+(?:\.[a-z0-9-]+)*\.(?:com|net|org|io|dev|app|co|ai|us|uk|ca|cloud|local|internal|corp|biz|info|me|tech|xyz)\b/i;

// A blocked name matches as written, split on hyphens and underscores and joined with spaces,
// and as the prefix of a Jira key.
const partsOf = (term) => term.split(/[-_]/).filter((part) => part.length >= 3 && !STOP_SEGMENTS.has(part));
const wordRe = (source) => new RegExp(`(?<![a-z0-9])${source}(?![a-z0-9])`, "i");

export function scrub(path, terms = blockList()) {
  const patterns = [...terms].flatMap(([term, cls]) => {
    const parts = partsOf(term);
    return [
      [wordRe(escape(term)), cls],
      ...(parts.length > 1 ? [[wordRe(parts.map(escape).join("[-_\\s]+")), cls]] : []),
      ...parts.map((part) => [new RegExp(`(?<![A-Za-z0-9])${escape(part)}-\\d+(?![A-Za-z0-9])`, "i"), cls]),
    ];
  });
  const generic = [
    [new RegExp(JIRA_KEY.source), "jira"],
    [URL_RE, "url"],
    [HOST_RE, "host"],
  ];
  const passed = [];
  const rejected = [];
  readJsonl(path).forEach((row, index) => {
    const text = JSON.stringify(row);
    if (redact(text) !== text) return rejected.push({ index, class: "secret" });
    const hit = [...patterns, ...generic].find(([pattern]) => pattern.test(text));
    if (hit) return rejected.push({ index, class: hit[1] });
    passed.push(row);
  });
  return { passed, rejected };
}

async function main([command, ...rest]) {
  // --only <dir>[,<dir>] limits scan and classify to those project directories.
  const at = rest.indexOf("--only");
  const only = at >= 0 ? String(rest[at + 1] ?? "").split(",").filter(Boolean) : undefined;
  if (at >= 0 && !only.length) throw new Error("usage: --only <dir>[,<dir>]");
  const args = at >= 0 ? rest.filter((_, i) => i !== at && i !== at + 1) : rest;
  const commands = {
    scan: () => scan({ only }),
    classify: () => classify({ only }),
    sample: () => sample(args[0] ? Number(args[0]) : SAMPLE),
    answer: () => answer(...args),
    check: () => check(),
    report: () => report(),
    points: () => points(),
    candidate: () => candidate(args[0], args[1], args[2], args[3], args[0] === "set" && args[2] === "proposed" ? JSON.parse(readFileSync(0, "utf8") || "{}") : {}),
    scrub: () => scrub(args[0]),
  };
  if (!commands[command]) throw new Error(`usage: node history.mjs <${Object.keys(commands).join("|")}> [args]`);
  console.log(JSON.stringify(await commands[command]()));
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    await main(process.argv.slice(2));
  } catch (error) {
    console.error(String(error?.message ?? error));
    process.exitCode = 1;
  }
}
