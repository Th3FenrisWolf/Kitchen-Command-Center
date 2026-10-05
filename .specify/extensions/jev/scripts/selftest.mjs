import assert from "node:assert/strict";
import { questionHash, validateBank, verdict, worst, ask, loadBank, MODEL, PLUGIN_BANK } from "./core.mjs";
import { decide } from "./decide.mjs";
import { sentences } from "./ste100.mjs";
import { mkdirSync, mkdtempSync, writeFileSync } from "node:fs";
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
  assert.deepEqual(merged.mode, { stop: "off", ask: "on", prompt: "on", session: "on" });
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

  // Session: every failed probe is listed with its fix.
  const session = steerSession({}, { config, run: () => false });
  assert.match(session.output.hookSpecificOutput.additionalContext, /GitHub: `gh auth status` failed\. Fix: `gh auth login`/);
}
delete process.env.JEV_API_KEY;

console.log("jev selftest: all assertions passed");
