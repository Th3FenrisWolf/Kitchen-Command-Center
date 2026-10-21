import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { questionHash, validateBank, verdict, worst, ask, loadBank, MODEL, PLUGIN_BANK } from "./core.mjs";
import { decide } from "./decide.mjs";
import { sentences } from "./ste100.mjs";
import { mkdirSync, mkdtempSync, readFileSync, unlinkSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { cases, loadConfig, modeFor, redact, steerAsk, steerAskResult, steerPrompt, steerSession, steerStop } from "./steer.mjs";

const failsOnTrue = { id: "a", primitive: "noul", text: "t", band: [0.35, 0.75], failWhen: "true" };
const failsOnFalse = { ...failsOnTrue, id: "b", failWhen: "false" };
const bankOf = (...questions) => ({ model: MODEL, questions });

// Polarity: the same probability is a pass or a fail depending on the question.
assert.equal(verdict(0.95, failsOnTrue), "fail");
assert.equal(verdict(0.05, failsOnTrue), "pass");
assert.equal(verdict(0.95, failsOnFalse), "pass");
assert.equal(verdict(0.05, failsOnFalse), "fail");
assert.equal(verdict(0.5, failsOnTrue), "review");
assert.equal(verdict(0.35, failsOnTrue), "review", "the band is inclusive at both edges");
assert.equal(verdict(0.75, failsOnTrue), "review");

assert.equal(worst(["pass", "review", "fail"]), "fail");
assert.equal(worst(["pass", "review"]), "review");
assert.equal(worst(["pass", "pass"]), "pass");
assert.equal(worst([]), "skipped");

// A hash covers the semantic fields and nothing else, so editing a question
// invalidates its recorded verdicts and renaming its rule does not.
assert.equal(questionHash(failsOnTrue), questionHash({ ...failsOnTrue, rule: "anything" }));
assert.notEqual(questionHash(failsOnTrue), questionHash({ ...failsOnTrue, text: "u" }));
assert.notEqual(questionHash(failsOnTrue), questionHash(failsOnFalse));
assert.notEqual(questionHash(failsOnTrue), questionHash({ ...failsOnTrue, band: [0.4, 0.6] }));
assert.notEqual(
  questionHash(failsOnTrue),
  questionHash({ ...failsOnTrue, criteria: { true: "x", false: "y" } }),
  "criteria reach the model, so they belong in the hash",
);

assert.deepEqual(validateBank(bankOf(failsOnTrue)), []);
assert.match(validateBank({ model: "jev-0.9", questions: [] })[0], /calibrate before bumping/);
assert.match(validateBank(bankOf({ ...failsOnTrue, failWhen: undefined }))[0], /needs failWhen/);
assert.match(validateBank(bankOf({ ...failsOnTrue, band: [0.8, 0.2] }))[0], /low below high/);
assert.match(validateBank(bankOf({ ...failsOnTrue, hash: "deadbeef" }))[0], /hash is stale/);
assert.match(validateBank(bankOf(failsOnTrue, failsOnTrue))[0], /duplicate id/);
assert.match(
  validateBank(bankOf({ ...failsOnTrue, criteria: { true: "only one side" } }))[0],
  /both a true and a false/,
);

// The API requires criteria on a choice and on a score, with its own limits.
const choice = { id: "c", primitive: "choice", text: "t", criteria: { one: null, two: null } };
assert.deepEqual(validateBank(bankOf(choice)), []);
assert.match(validateBank(bankOf({ ...choice, criteria: { only: null } }))[0], /two or more/);
const many = Object.fromEntries(Array.from({ length: 256 }, (_, i) => [`o${i}`, null]));
assert.match(validateBank(bankOf({ ...choice, criteria: many }))[0], /at most 255/);

const score = { id: "s", primitive: "score", text: "t", criteria: ["low", "high"] };
assert.deepEqual(validateBank(bankOf(score)), []);
assert.match(validateBank(bankOf({ ...score, criteria: ["only"] }))[0], /2 to 10 level/);
assert.match(
  validateBank(bankOf({ ...score, criteria: Array.from({ length: 11 }, () => "l") }))[0],
  /2 to 10 level/,
);
assert.match(validateBank(bankOf({ ...failsOnTrue, primitive: "guess" }))[0], /must be noul/);

// The transport answers under the keys the caller chose, so order never matters.
const reply = (noul) => async (_state, questions) => ({
  model: MODEL,
  answers: Object.fromEntries(questions.map((q) => [q.id, { type: "noul", noul }])),
  usage: { input_tokens: 100, output_tokens: 0 },
});

process.env.JEV_API_KEY = "selftest";
const pair = await ask("state", [failsOnTrue, failsOnFalse], { transport: reply(0.95) });
assert.deepEqual(
  pair.map((answer) => [answer.id, answer.verdict]),
  [
    ["a", "fail"],
    ["b", "pass"],
  ],
);
assert.equal(pair[0].inputTokens, 100);

// A split sample is a review, not a majority vote.
const series = [0.9, 0.1, 0.9];
let call = 0;
const split = await ask("state", [failsOnTrue], {
  samples: 3,
  transport: async (state, questions) => reply(series[call++])(state, questions),
});
assert.equal(split[0].verdict, "review");
assert.equal(split[0].agreed, false);
assert.equal(split[0].inputTokens, 300);

const agreedRun = await ask("state", [failsOnTrue], { samples: 3, transport: reply(0.95) });
assert.equal(agreedRun[0].verdict, "fail");
assert.equal(agreedRun[0].agreed, true);

// A choice returns its option, and a split choice is a review.
const picks = ["rule", "fact"];
let pick = 0;
const choiceReply = async (_state, questions) => ({
  model: MODEL,
  answers: Object.fromEntries(
    questions.map((q) => [q.id, { type: "choice", choice: picks[pick++ % 2], confidence: 0.8 }]),
  ),
  usage: {},
});
const picked = await ask("state", [choice], { transport: choiceReply });
assert.equal(picked[0].verdict, "rule");
assert.equal(picked[0].confidence, 0.8);
const splitChoice = await ask("state", [choice], { samples: 2, transport: choiceReply });
assert.equal(splitChoice[0].verdict, "review");

// A served model that differs from the pin is a stop, not a warning.
await assert.rejects(
  ask("state", [failsOnTrue], {
    transport: async () => ({ model: "jev-2.0.0", answers: {}, usage: {} }),
  }),
  /recalibrate first/,
);

// decide: floor cases run first and never reach the transport.
const tier = {
  ...failsOnTrue,
  id: "tier",
  floor: [
    { match: "\\[OWNER\\]", then: "owner" },
    { match: "^create", then: "code" },
  ],
  then: { pass: "sonnet", fail: "opus" },
  fallback: "opus",
};
const unreachable = async () => assert.fail("a floor case must not call Jev");
assert.deepEqual(await decide("- [OWNER] ask the client", tier, { transport: unreachable }), {
  outcome: "owner",
  via: "floor",
  reason: "\\[OWNER\\]",
});
assert.equal((await decide("Create the folder", tier, { transport: unreachable })).outcome, "code");
assert.equal((await decide({ task: "[owner] x" }, tier, { transport: unreachable })).outcome, "owner");

// then maps the verdict to an outcome.
assert.equal((await decide("edit a file", tier, { transport: reply(0.05) })).outcome, "sonnet");
assert.equal((await decide("edit a file", tier, { transport: reply(0.95) })).outcome, "opus");

// Inside the band, a split sample and low confidence all take the fallback.
const band = await decide("edit a file", tier, { transport: reply(0.5) });
assert.deepEqual([band.outcome, band.via], ["opus", "fallback"]);
assert.equal((await decide("x", { ...tier, fallback: undefined }, { transport: reply(0.5) })).outcome, "review");
const shaky = await decide("x", { ...choice, minConfidence: 0.9 }, { transport: choiceReply });
assert.deepEqual([shaky.outcome, shaky.via], ["review", "fallback"]);
assert.equal((await decide("x", choice, { transport: choiceReply })).via, "jev");

delete process.env.JEV_API_KEY;
const keyless = await decide("edit a file", tier, { transport: unreachable });
assert.deepEqual([keyless.outcome, keyless.via], ["opus", "fallback"]);

// floor, then, fallback, minConfidence and blocking never reach the model, so
// they stay out of the hash and an edit to them keeps the recorded verdicts.
assert.equal(questionHash(tier), questionHash(failsOnTrue));
assert.equal(questionHash({ ...failsOnTrue, blocking: true, minConfidence: 0.5 }), questionHash(failsOnTrue));
assert.deepEqual(validateBank(bankOf(tier)), []);
assert.match(validateBank(bankOf({ ...tier, floor: [{ match: "(" , then: "x" }] }))[0], /valid regex/);
assert.match(validateBank(bankOf({ ...tier, floor: [{ match: "x" }] }))[0], /needs a then/);
assert.match(validateBank(bankOf({ ...choice, then: { pass: "x" } }))[0], /only a noul/);

// The shipped bank loads, and every question carries a rubric and a blocking flag.
const bank = loadBank([PLUGIN_BANK]);
assert.ok(bank.questions.length >= 8);
for (const question of bank.questions) {
  assert.ok(question.criteria, `${question.id} has no criteria`);
  assert.ok(question.scope, `${question.id} has no scope`);
  assert.equal(typeof question.blocking, "boolean", `${question.id} has no blocking flag`);
}
// Only prose reaches a question. A table row, a heading, a fenced block and a
// short fragment are not sentences.
const markdown = [
  "# A heading that is long enough to look like prose",
  "",
  "The seed leaves found content alone. It does not overwrite it, it skips it.",
  "",
  "| Column | Meaning |",
  "|---|---|",
  "| a | a table row that is long enough to look like prose |",
  "",
  "```",
  "This fenced line reads like prose but it is code.",
  "```",
  "",
  "Too short.",
  "Run the `pnpm baseline` script and read [the standard](standards/a.md) first.",
  "",
  "- The first bullet says one whole thing about the seed.",
  "- The second bullet says a different whole thing about the router.",
].join("\n");

const found = sentences(markdown);
assert.ok(
  found.some((sentence) => sentence.includes("The seed leaves found content alone")),
  "prose is kept",
);
assert.ok(
  found.some((sentence) => sentence.includes("does not overwrite it")),
  "two sentences on one line both survive",
);
assert.ok(!found.some((sentence) => sentence.includes("a table row")), "a table row is dropped");
assert.ok(!found.some((sentence) => sentence.includes("look like prose but")), "a fence is dropped");
assert.ok(!found.some((sentence) => sentence.includes("A heading that is")), "a heading is dropped");
assert.ok(!found.some((sentence) => sentence === "Too short."), "a short fragment is dropped");

const linked = found.find((sentence) => sentence.includes("Run the"));
assert.match(linked, /CODE/, "inline code becomes a placeholder");
assert.match(linked, /the standard first/, "a link keeps its text and drops its target");

// Each bullet is judged on its own. Merged bullets would ask one question about
// several unrelated claims.
const bullets = found.filter((sentence) => sentence.includes("bullet says"));
assert.equal(bullets.length, 2);
assert.ok(bullets.every((sentence) => !sentence.includes("bullet says one whole thing about the seed. The second")));
assert.ok(bullets[0].startsWith("The first bullet"), "the list marker is stripped");


// Owner steering. Config merges plugin defaults, the user file, the project
// file and the environment, in that order.
{
  const home = mkdtempSync(join(tmpdir(), "steer-home-"));
  const project = mkdtempSync(join(tmpdir(), "steer-project-"));
  mkdirSync(join(home, ".claude"));
  mkdirSync(join(project, ".claude"));
  writeFileSync(join(home, ".claude", "steer.json"), JSON.stringify({ mode: { stop: "on" } }));
  writeFileSync(join(project, ".claude", "steer.json"), JSON.stringify({ mode: { ask: "on" }, floors: ["\\bGATED\\b"], probes: [{ name: "x", run: "rm -rf /", fix: "" }] }));
  const merged = loadConfig({ env: { BIZSTREAM_STEER_STOP: "off" }, projectDir: project, home });
  assert.deepEqual(merged.mode, { stop: "off", ask: "on", prompt: "shadow", session: "on" });
  assert.deepEqual(merged.floors, ["\\bGATED\\b"]);
  assert.deepEqual(merged.probes.map((probe) => probe.name), ["GitHub"], "a project file never supplies a shell command");
  assert.equal(loadConfig({ env: { BIZSTREAM_STEER: "off" }, projectDir: project, home }).mode.session, "off");

  // A Jev steer with no key, or with jev turned off, changes nothing; the
  // preflight needs no key.
  assert.equal(modeFor("ask", merged, {}), "off");
  assert.equal(modeFor("ask", merged, { JEV_API_KEY: "k" }), "on");
  assert.equal(modeFor("ask", { ...merged, jev: false }, { JEV_API_KEY: "k" }), "off");
  assert.equal(modeFor("session", merged, {}), "on");
}

assert.match(redact("token=abc123 and ghp_0123456789abcdefghijklmn and a@b.com"), /token=\[redacted\] and \[redacted\] and \[email\]/);

process.env.JEV_API_KEY = "selftest";
{
  const shipped = loadBank([PLUGIN_BANK]);
  const config = loadConfig({ env: {}, projectDir: tmpdir(), home: tmpdir() });
  const deps = (mode, transport = unreachable) => ({ config, mode, bank: shipped, transport });

  // Stop: the loop guard and the floors never reach Jev.
  assert.deepEqual(await steerStop({ stop_hook_active: true, last_assistant_message: "x" }, deps("on")), { row: null, output: null });
  const waiting = await steerStop({ last_assistant_message: "needs input: pick a region" }, deps("on"));
  assert.deepEqual([waiting.row.outcome, waiting.row.via, waiting.output], ["stop", "floor", null]);
  const short = { last_assistant_message: "Tests pass. Want me to push the branch and open the pull request?" };
  const blocked = await steerStop(short, deps("on", reply(0.95)));
  assert.equal(blocked.output.decision, "block");
  const shadowStop = await steerStop(short, deps("shadow", reply(0.95)));
  assert.deepEqual([shadowStop.row.outcome, shadowStop.output], ["continue", null], "shadow logs and never blocks");
  assert.equal((await steerStop(short, deps("on", reply(0.4)))).output, null, "an unsure answer lets the stop happen");

  // Ask: only a question with a recommended option reaches Jev, and the
  // answer comes back through updatedInput.
  const tool_input = {
    questions: [{ question: "Which runner?", header: "Runner", multiSelect: false, options: [{ label: "Node (Recommended)", description: "a" }, { label: "Bun", description: "b" }] }],
  };
  const answered = await steerAsk({ tool_input, tool_use_id: "t1" }, deps("on", reply(0.95)));
  assert.equal(answered.output.hookSpecificOutput.permissionDecision, "allow");
  assert.deepEqual(answered.output.hookSpecificOutput.updatedInput, { ...tool_input, answers: { "Which runner?": "Node (Recommended)" } });
  assert.equal((await steerAsk({ tool_input }, deps("shadow", reply(0.95)))).output, null);
  assert.equal((await steerAsk({ tool_input }, deps("on", reply(0.05)))).output, null, "a predicted override goes to the owner");
  const bare = { questions: [{ ...tool_input.questions[0], options: [{ label: "Node" }, { label: "Bun" }] }] };
  assert.equal((await steerAsk({ tool_input: bare }, deps("on"))).row.predictions[0].reason, "no recommended option");
  const typo = await steerAsk({ tool_input }, { ...deps("on", reply(0.95)), config: { ...config, floors: ["(", "Runner"] } });
  assert.equal(typo.row.predictions[0].reason, "Runner", "an invalid floor is skipped and the next one still applies");
  const risky = { questions: [{ ...tool_input.questions[0], question: "Which runner do we deploy with?" }] };
  assert.equal((await steerAsk({ tool_input: risky }, deps("on"))).row.predictions[0].via, "floor");
  const closeOut = { questions: [{ ...tool_input.questions[0], question: "Publish the close-out: push, open the pull request, move the card?" }] };
  assert.equal((await steerAsk({ tool_input: closeOut }, deps("on", reply(0.95)))).output, null, "card implement's close-out always reaches the owner");

  // Each answered question becomes a case, and a shadow stop followed by a
  // nudge becomes a stopped-short case.
  const result = steerAskResult({ tool_input, tool_use_id: "t1", tool_response: { answers: { "Which runner?": "Bun" } } }).row;
  const stopRow = { session: "s", ...shadowStop.row };
  const nudge = { session: "s", event: "prompt", intent: "proceed" };
  assert.deepEqual(
    cases([{ session: "s", ...result }, stopRow, nudge]).map((c) => [c.question, c.expected]),
    [["steer-owner-takes-default", "fail"], ["steer-stopped-short", "fail"]],
  );
  const echoed = steerAskResult({ tool_input, tool_use_id: "t1", tool_response: { answers: { "Which runner?": "Node (Recommended)" } } }).row;
  assert.deepEqual(cases([{ session: "s", ...answered.row }, { session: "s", ...echoed }]), [], "a steer answer is no evidence");

  // Prompt: a correction adds context in on mode.
  const byType = async (_state, questions) => ({
    model: MODEL,
    answers: Object.fromEntries(
      questions.map((q) => [q.id, q.primitive === "choice" ? { type: "choice", choice: q.id === "owner-intent" ? "correction" : "forgot-rule", confidence: 0.9 } : { type: "noul", noul: 0.9 }]),
    ),
    usage: {},
  });
  const corrected = await steerPrompt({ prompt: "you forgot the rule again" }, deps("on", byType));
  assert.deepEqual([corrected.row.intent, corrected.row.fault, corrected.row.routine], ["correction", "forgot-rule", "fail"]);
  assert.match(corrected.output.hookSpecificOutput.additionalContext, /write the rule/);
  assert.equal((await steerPrompt({ prompt: "you forgot" }, deps("shadow", byType))).output, null);
  // Text the harness injects is not the owner: no row, no Jev call.
  const noCall = async () => assert.fail("Jev called for a prompt the owner did not type");
  for (const injected of ['<task-notification>\n<task-id>a1</task-id>', '<agent-message from="tests">\n[Subagent hand-back] done', "<teammate-message teammate_id=\"bash\">go", "<local-command-caveat>Caveat</local-command-caveat>", "<command-name>/clear</command-name>", "<local-command-stdout>ok</local-command-stdout>"]) {
    assert.deepEqual(await steerPrompt({ prompt: injected }, deps("on", noCall)), { row: null, output: null }, injected);
  }

  // Session: every failed probe is listed with its fix.
  const session = steerSession({}, { config, run: () => false });
  assert.match(session.output.hookSpecificOutput.additionalContext, /GitHub: `gh auth status` failed\. Fix: `gh auth login`/);
}
delete process.env.JEV_API_KEY;

// History reader: fixture transcripts in a temp home, Jev faked by marker words.
{
  const home = mkdtempSync(join(tmpdir(), "jev-history-"));
  const root = join(home, "projects");
  process.env.BIZSTREAM_STEER_DIR = join(home, "steer");
  process.env.BIZSTREAM_HISTORY_ROOT = root;
  const app = "/nonexistent/clientco/app";
  const human = (sessionId, uuid, cwd, branch, content) => ({ type: "user", sessionId, uuid, cwd, gitBranch: branch, timestamp: "2026-10-01T10:00:00Z", origin: { kind: "human" }, message: { role: "user", content } });
  const said = (sessionId, text, extra = []) => ({ type: "assistant", sessionId, cwd: app, message: { content: [{ type: "text", text }, ...extra] } });
  const write = (dir, name, rows) => {
    mkdirSync(join(root, dir), { recursive: true });
    writeFileSync(join(root, dir, name), rows.map((r) => (typeof r === "string" ? r : JSON.stringify(r))).join("\n"));
  };
  write("proj-a", "a.jsonl", [
    human("A", "a1", app, "feat-1", "NEW build feature one"),
    said("A", "working on it"),
    { type: "user", sessionId: "A", message: { content: [{ type: "tool_result", tool_use_id: "x", content: "ok" }] }, toolUseResult: { stdout: "ok" } },
    { type: "user", sessionId: "A", isMeta: true, message: { content: "injected" } },
    { type: "user", sessionId: "A", origin: { kind: "task-notification" }, message: { content: "<task-notification>done</task-notification>" } },
  ]);
  const ask = { type: "tool_use", id: "toolu_1", name: "AskUserQuestion", input: { questions: [{ question: "Ship it?", header: "Ship", options: [{ label: "Yes (Recommended)" }, { label: "No" }] }] } };
  write("proj-a", "b.jsonl", [
    human("B", "b1", `${app}/.claude/worktrees/w1`, "feat-2", "NEW pick the layout"),
    said("B", "built it for ACME-7", [ask]),
    { type: "user", sessionId: "B", message: { content: [{ type: "tool_result", tool_use_id: "toolu_1", content: "answered" }] }, toolUseResult: { questions: [], answers: { "Ship it?": "Yes (Recommended)" } } },
    human("B", "b2", `${app}/.claude/worktrees/w1`, "feat-2", "that is wrong, the term means a client"),
    said("B", "fixed"),
    human("B", "b3", `${app}/.claude/worktrees/w1`, "feat-2", "NEW maybe look at another thing"),
    said("B", "looked"),
  ]);
  write("proj-b", "c.jsonl", ["{not json", human("C", "c1", "/nonexistent/other/repo", "main", "use sk-abcdefghijklmnopqrstuv and mail dev@client.example"), said("C", "done")]);
  mkdirSync(join(root, "proj-b", "c", "subagents"), { recursive: true });
  writeFileSync(join(root, "proj-b", "c", "subagents", "agent-1.jsonl"), JSON.stringify(human("S", "s1", app, "main", "NEW subagent")));

  const history = await import("./history.mjs");
  assert.equal(history.listTranscripts().length, 3, "subagent transcripts are left out");
  const parsedA = history.parseSession(readFileSync(join(root, "proj-a", "a.jsonl"), "utf8"));
  assert.equal(parsedA.humanTurns.length, 1, "tool results, meta rows and notifications are not human turns");
  assert.equal(history.parseSession(readFileSync(join(root, "proj-b", "c.jsonl"), "utf8")).skipped, 1);
  assert.equal(history.scan().turns, 5);
  assert.equal(history.scan().sessions, 3);

  let calls = 0;
  const humanPart = (state) => state.split("\nHuman")[1] ?? state;
  const fake = async (state, questions) => {
    calls += 1;
    const said = humanPart(state);
    const answers = {};
    for (const q of questions) {
      if (q.id === "history-turn-class") {
        const choice = /wrong/.test(said) ? "misread" : /pick/.test(said) ? "decision" : "other";
        answers[q.id] = { type: "choice", choice, confidence: /maybe/.test(said) ? 0.3 : 0.9 };
      } else if (q.id === "history-task-start") answers[q.id] = { type: "noul", noul: /NEW/.test(said) ? 0.9 : 0.1 };
      else answers[q.id] = { type: "noul", noul: /wrong/.test(state) ? 0.1 : 0.9 };
    }
    return { model: MODEL, answers, usage: { input_tokens: 1 } };
  };
  delete process.env.JEV_API_KEY;
  await assert.rejects(history.classify({ transport: fake }), /JEV_API_KEY/);
  assert.equal(calls, 0, "no key, no request");
  process.env.JEV_API_KEY = "selftest";
  assert.deepEqual(await history.classify({ transport: fake }), { turns: 5, tasks: 4, review: 1, errors: 0 });
  assert.equal(history.scan().turns, 0, "a second scan finds nothing new");

  const dir = join(home, "steer", "history");
  const turns = history.readJsonl(join(dir, "turns.jsonl"));
  const byUuid = (u) => turns.find((t) => t.id.endsWith(`:${u}`));
  assert.equal(byUuid("a1").repo, byUuid("b1").repo, "a worktree counts as its repository");
  assert.equal(byUuid("b3").class, "review", "a choice under minConfidence is a review");
  assert.deepEqual([byUuid("b1").first, byUuid("b2").first, byUuid("b3").first], [true, false, true]);
  const [misread] = history.readJsonl(join(dir, "misreads.jsonl"));
  assert.deepEqual([misread.turnsTouched, misread.cardsTouched], [2, 2], "branch feat-2 and ACME-7");

  const first = history.report();
  assert.equal(first.provisional, true);
  assert.equal(first.gate, "open", "1 misread in 4 decided turns");
  for (const name of ["turns.jsonl", "tasks.jsonl", "misreads.jsonl", "report.md"]) {
    const text = readFileSync(join(dir, name), "utf8");
    assert.doesNotMatch(text, /sk-abcdef|dev@client|build feature one/, `${name} holds no prompt text or secret`);
  }

  // Gate thresholds and agreement arithmetic.
  const rows = (n, cls) => Array.from({ length: n }, () => ({ class: cls }));
  assert.equal(history.gate([...rows(12, "misread"), ...rows(88, "other")], []).state, "open");
  assert.equal(history.gate(rows(10, "other"), [{ cardsTouched: 2 }]).state, "open");
  assert.equal(history.gate([...rows(9, "misread"), ...rows(91, "other")], [{ cardsTouched: 1 }]).state, "closed");
  const answered = (agree, total) => Array.from({ length: total }, (_, i) => ({ sampled: "random", class: "other", owner: { class: i < agree ? "other" : "lookup" } }));
  assert.equal(history.agreementOf([...answered(23, 30), { sampled: "review", class: "review", owner: { class: "lookup" } }]).randomSampled, 30, "review items never count");
  assert.ok(history.agreementOf(answered(23, 30)).agreement < 0.8);

  // Owner sample: review first, then random; one answer on a task's first turn writes three rows.
  const { items } = history.sample(2);
  assert.equal(items.filter((i) => i.sampled === "review").length, 1);
  assert.equal(items.filter((i) => i.sampled === "random").length, 2);
  const firstTurn = history.sample(30).items.find((i) => i.id.endsWith(":a1"));
  history.answer(firstTurn.id, "decision", "pass", "pass");
  const owned = history.readJsonl(join(dir, "cases.jsonl"));
  assert.deepEqual(owned.map((r) => r.question), history.QUESTIONS);
  assert.throws(() => history.answer(firstTurn.id, "other", "pass"), /already answered/);
  assert.throws(() => history.answer(byUuid("b2").id, "nonsense", "pass"), /class must be one of/);

  // A grown session is classified again and keeps the owner's answers.
  const grown = join(root, "proj-a", "a.jsonl");
  writeFileSync(grown, `${readFileSync(grown, "utf8")}\n${JSON.stringify(human("A", "a2", app, "feat-1", "also add tests"))}`);
  await history.classify({ transport: fake });
  const regrown = history.readJsonl(join(dir, "turns.jsonl")).find((t) => t.id === firstTurn.id);
  assert.deepEqual([regrown.sampled, regrown.owner?.class], ["random", "decision"]);
  assert.equal(history.readJsonl(join(dir, "tasks.jsonl")).find((t) => t.firstTurn === firstTurn.id).owner, "pass");

  // check: under 20 cases or one miss fails; 20 right cases pass.
  assert.equal((await history.check({ transport: fake })).pass, false);
  const twenty = Array.from({ length: 20 }, () => ({ question: "history-turn-class", expected: "other", state: "Human:\nhello" }));
  writeFileSync(join(dir, "cases.jsonl"), twenty.map((r) => JSON.stringify(r)).join("\n"));
  assert.equal((await history.check({ transport: fake })).questions["history-turn-class"].pass, true);
  writeFileSync(join(dir, "cases.jsonl"), [...twenty, { question: "history-turn-class", expected: "lookup", state: "Human:\nhello" }].map((r) => JSON.stringify(r)).join("\n"));
  assert.deepEqual((await history.check({ transport: fake })).questions["history-turn-class"], { cases: 21, misses: 1, pass: false });

  // Decision points: an ask with its answer, dropped when the steer answered it, and gone once rejected.
  const askPoints = (steerRows) => history.points({ steerRows }).points.filter((p) => p.kind === "ask");
  const [point] = askPoints([]);
  assert.deepEqual([point.recommended, point.chosen, point.repo], ["Yes (Recommended)", "Yes (Recommended)", byUuid("a1").repo]);
  assert.equal(askPoints([{ event: "ask", answered: true, tool_use_id: "toolu_1" }]).length, 0);
  assert.ok(history.points({ steerRows: [] }).points.some((p) => p.kind === "misread"));
  history.candidate("set", "ship-check", "proposed", null, { scope: "specific", points: [point.id] });
  assert.throws(() => history.candidate("set", "ship-check", "banked"), /cannot move/);
  history.candidate("set", "ship-check", "rejected", "owner");
  assert.equal(askPoints([]).length, 0, "a rejected candidate's points are not proposed again");

  // Scrub: a path segment and a secret are rejected by class, never by term.
  const scrubFile = join(home, "rows.jsonl");
  writeFileSync(scrubFile, ['{"state":"the clientco catalog"}', '{"state":"mail a@b.co"}', '{"state":"a neutral row"}'].join("\n"));
  const scrubbed = history.scrub(scrubFile);
  assert.deepEqual(scrubbed.rejected, [{ index: 0, class: "path" }, { index: 1, class: "secret" }]);
  assert.equal(scrubbed.passed.length, 1);
  assert.doesNotMatch(JSON.stringify(scrubbed.rejected), /clientco/);

  // Scrub: a blocked name also matches split, joined with spaces, and as a Jira key prefix; any key, URL or host goes.
  const blocked = new Map([["acme-portal", "path"]]);
  writeFileSync(
    scrubFile,
    ["the acme portal page", "see ACME-123", "ticket PROJ-9", "visit https://x.example/y", "mail the foo.example.com host", "a neutral row"].map((state) => JSON.stringify({ state })).join("\n"),
  );
  assert.deepEqual(history.scrub(scrubFile, blocked).rejected.map((r) => r.class), ["path", "path", "jira", "url", "host"]);

  // A message of text and an image is a human turn; an image alone is not.
  const mixed = [{ type: "text", text: "NEW look at this" }, { type: "image", source: {} }];
  write("proj-a", "d.jsonl", [human("D", "d1", app, "main", mixed), said("D", "ok"), human("D", "d2", app, "main", [{ type: "image", source: {} }])]);
  const parsedD = history.parseSession(readFileSync(join(root, "proj-a", "d.jsonl"), "utf8"));
  assert.deepEqual(parsedD.humanTurns.map((t) => t.prompt), ["NEW look at this"]);

  // Two transcripts with one sessionId keep their own rows. A run writes each session as it finishes.
  write("proj-b", "e.jsonl", [human("A", "e1", app, "main", "NEW twin of session A")]);
  const rowsBefore = history.readJsonl(join(dir, "turns.jsonl")).filter((t) => t.session === "A").length;
  let sawFirst = null;
  const watching = async (state, questions) => {
    if (/SECOND/.test(state)) {
      await new Promise((resolve) => setTimeout(resolve, 80));
      sawFirst ??= history.readJsonl(join(dir, "turns.jsonl")).some((t) => t.session === "F");
    }
    return fake(state, questions);
  };
  write("proj-a", "f.jsonl", [human("F", "f1", app, "main", "NEW FIRST task")]);
  write("proj-a", "g.jsonl", [human("G", "g1", app, "main", "NEW SECOND task")]);
  await history.classify({ transport: watching });
  assert.equal(sawFirst, true, "a finished session is on disk while another still runs");
  const afterTwin = history.readJsonl(join(dir, "turns.jsonl")).filter((t) => t.session === "A");
  assert.equal(afterTwin.length, rowsBefore + 1, "the twin adds its row and drops none");
  assert.deepEqual(new Set(history.readJsonl(join(dir, "tasks.jsonl")).filter((t) => t.session === "A").map((t) => t.file)), new Set([join(root, "proj-a", "a.jsonl"), join(root, "proj-b", "e.jsonl")]));

  // Old-shape rows (no file on a task) are still read and replaced by session.
  const legacy = history.readJsonl(join(dir, "tasks.jsonl")).map(({ file: _file, ...task }) => task);
  history.writeJsonl(join(dir, "tasks.jsonl"), legacy);
  writeFileSync(join(root, "proj-b", "e.jsonl"), `${readFileSync(join(root, "proj-b", "e.jsonl"), "utf8")}\n${JSON.stringify(human("A", "e2", app, "main", "more"))}`);
  await history.classify({ transport: fake });
  assert.ok(history.readJsonl(join(dir, "tasks.jsonl")).every((t) => t.session !== "A" || t.file), "legacy rows of the regrown session are replaced");

  // answer needs every answer the turn needs, and writes nothing otherwise.
  const casesBefore = readFileSync(join(dir, "cases.jsonl"), "utf8");
  const turnsBefore = readFileSync(join(dir, "turns.jsonl"), "utf8");
  const gFirst = history.readJsonl(join(dir, "turns.jsonl")).find((t) => t.id === "G:g1");
  assert.throws(() => history.answer(gFirst.id, "other"), /usage: answer/);
  assert.throws(() => history.answer(gFirst.id, "other", "pass"), /usage: answer/);
  assert.equal(readFileSync(join(dir, "cases.jsonl"), "utf8"), casesBefore);
  assert.equal(readFileSync(join(dir, "turns.jsonl"), "utf8"), turnsBefore);

  // A deleted transcript, or a turn gone from one, is skipped, never thrown.
  write("proj-a", "h.jsonl", [human("H", "h1", app, "main", "NEW to be deleted"), human("H", "h2", app, "main", "NEW to be trimmed")]);
  await history.classify({ transport: fake });
  history.sample(1000);
  unlinkSync(join(root, "proj-a", "f.jsonl"));
  writeFileSync(join(root, "proj-a", "h.jsonl"), JSON.stringify(human("H", "h2", app, "main", "NEW to be trimmed")));
  const left = history.sample(1000).items.map((i) => i.id);
  assert.ok(!left.includes("F:f1") && !left.includes("H:h1") && left.includes("H:h2"));
  const casesNow = readFileSync(join(dir, "cases.jsonl"), "utf8");
  history.answer("F:f1", "other", "pass", "pass");
  assert.equal(readFileSync(join(dir, "cases.jsonl"), "utf8"), casesNow, "a skipped answer writes nothing");
  history.points({ steerRows: [] });

  // scan names the project directories it will read, and --only limits it.
  write("proj-c", "k.jsonl", [human("K", "k1", app, "main", "NEW more")]);
  const scanned = history.scan();
  assert.deepEqual(scanned.projects.map((p) => p.dir).sort(), ["proj-a", "proj-c"]);
  assert.match(scanned.costNote, /lower bound/);
  assert.deepEqual(history.scan({ only: ["proj-c"] }).projects, [{ dir: "proj-c", sessions: 1, turns: 1 }]);
  await history.classify({ transport: fake, only: ["proj-a"] });
  assert.deepEqual(history.scan().projects, [{ dir: "proj-c", sessions: 1, turns: 1 }], "--only left proj-c unread");

  // A cwd that no longer exists is unresolved; one that exists is not.
  assert.match(history.repoKey("/nonexistent/clientco/app"), /^unresolved:/);
  assert.doesNotMatch(history.repoKey(home), /^unresolved:/);

  assert.equal(history.normalizeRemote("git@github.com:Acme/App.git"), "github.com/acme/app");
  assert.equal(history.normalizeRemote("https://user@github.com/Acme/App.git"), "github.com/acme/app");
  assert.match(history.repoKey("/nonexistent/.claude/jobs/1/tmp/wt-x"), /^unresolved:/);

  delete process.env.BIZSTREAM_STEER_DIR;
  delete process.env.BIZSTREAM_HISTORY_ROOT;
  delete process.env.JEV_API_KEY;
}

// Imported with no script path and no key, no script runs its command line,
// and the key stays unset.
{
  const env = { ...process.env };
  delete env.JEV_API_KEY;
  delete env.EVAL_JEV_API_KEY;
  const imports = ["core", "decide", "steer", "ste100", "doc-kinds", "history"].map((name) => `await import(${JSON.stringify(new URL(`./${name}.mjs`, import.meta.url).href)});`);
  const probe = `${imports.join(" ")} console.log(String(process.env.JEV_API_KEY === undefined));`;
  assert.equal(execFileSync(process.execPath, ["--input-type=module", "-e", probe], { env, encoding: "utf8" }).trim(), "true");
}

console.log("jev selftest: all assertions passed");
