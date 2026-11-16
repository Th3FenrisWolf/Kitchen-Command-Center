import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  addRow,
  answer,
  checkpoints,
  featureDir,
  loadState,
  markerRows,
  markers,
  next,
  record,
  release,
  status,
  tasksState,
  unreviewedAssumptions,
} from "./flow.mjs";

// Row tags. T017 and T018 hang on a person row, as on the card that looped
// twelve times with no progress, so they are not runnable.
const tasks = `
- [X] T001 Set up
- [ ] T002 [P] Build the list in src/list.js
- [ ] T003 Depends on T002: wire the list into the page
- [ ] T016 [PERSON] Ask the client for the feed URL
- [ ] T017 Only if T016 found a feed, parse it
- [ ] T018 [WAITS:T017] Show the parsed feed
- [ ] T019 Price the tier (GATED: sales)
- [x] T020 Docs
`;
const state = tasksState(tasks);
assert.equal(state.total, 8);
assert.equal(state.done, 2);
assert.deepEqual(state.open, { runnable: ["T002", "T003"], waits: ["T017", "T018"], person: ["T016"], gated: ["T019"] });
assert.deepEqual(state.edges, [
  { row: "T003", on: ["T002"] },
  { row: "T017", on: ["T016"] },
  { row: "T018", on: ["T017"] },
]);
assert.deepEqual(tasksState(null), { total: 0, done: 0, open: { runnable: [], waits: [], person: [], gated: [] }, edges: [] });
// A wait on a done row, or on a row that does not exist, holds nothing.
assert.deepEqual(tasksState("- [x] T001 a\n- [ ] T002 Only if T001 or T099 passed").open.runnable, ["T002"]);
// A cycle does not hang the walk.
assert.equal(tasksState("- [ ] T001 Depends on T002\n- [ ] T002 Depends on T001").open.runnable.length, 2);

assert.deepEqual(unreviewedAssumptions("- A1: x\n- A2: y\nReviewed: 2026-10-26.\n- A3: z"), ["A3"]);
assert.deepEqual(unreviewedAssumptions("- A1: x\nReviewed: 2026-10-26."), []);
assert.deepEqual(unreviewedAssumptions(null), []);

// A project with no flow config and no qa extension.
const root = mkdtempSync(join(tmpdir(), "flow-selftest-"));
const dir = join(root, "specs", "001-feed");
mkdirSync(dir, { recursive: true });
const write = (name, text) => writeFileSync(join(dir, name), text);
const phase = () => next(dir, root).phase;

write("spec.md", "# Feature Specification: [FEATURE NAME]\n");
assert.equal(phase(), "specify", "an unfilled template is not a spec");
assert.equal(featureDir(join(dir, "spec.md")), dir, "a path inside a feature names the feature");

write("spec.md", "# Feed\n\n[NEEDS CLARIFICATION: which feed?]\n[NEEDS CLARIFICATION: GATED — sales: price]\n");
assert.deepEqual(markers(dir), { open: 1, gated: 1 });
assert.equal(phase(), "clarify");
write("spec.md", "# Feed\n\n## Assumptions\n\n- A1: Cache: one hour. Alternative: none.\n[NEEDS CLARIFICATION: GATED — sales: price]\n");
assert.equal(phase(), "plan", "a gated marker never holds the flow");
write("plan.md", "# Plan\n");
const review = next(dir, root);
assert.equal(review.phase, "assumption-review");
assert.deepEqual(review.unreviewed, ["A1"]);
write("spec.md", `${readFileSync(join(dir, "spec.md"), "utf8")}Reviewed: 2026-10-26. Shown to the owner: . Accepted without review: A1.\n`);
assert.equal(phase(), "tasks");
write("tasks.md", tasks);
assert.equal(phase(), "analyze", "analyze runs once and leaves no artifact, so only a record ends it");
record(dir, "analyze", "done");
const implement = next(dir, root);
assert.equal(implement.phase, "implement");
assert.deepEqual(implement.tasks.open.runnable, ["T002", "T003"]);

// Two failures in one phase hold the feature until the owner releases it.
assert.equal(record(dir, "implement", "failed", { note: "build broke" }).stop, false);
const held = record(dir, "implement", "failed", { note: "build broke" });
assert.equal(held.stop, true);
assert.equal(held.cause, "implement failed 2 times: build broke");
assert.equal(phase(), "held");
assert.equal(release(dir).phase, "implement");

write("tasks.md", tasks.replace("[ ] T002", "[X] T002").replace("[ ] T003", "[X] T003"));
assert.equal(next(dir, root).reason, "no pull request recorded", "only person, gated and waiting rows are left");
record(dir, "implement", "done", { pr: "42" });
assert.equal(phase(), "done", "no qa extension and publish ask skip the last three phases");

// publish: auto adds the merge phase.
mkdirSync(join(root, ".specify", "extensions", "flow"), { recursive: true });
writeFileSync(join(root, ".specify", "extensions", "flow", "flow-config.yml"), "publish: auto\n");
assert.equal(phase(), "merge");
record(dir, "merge", "done");
assert.equal(phase(), "done");
assert.deepEqual(status(join(root, "specs"), root), [{ feature: "001-feed", phase: "done", pr: 42, cause: null }]);

const saved = JSON.parse(readFileSync(join(dir, ".flow", "state.json"), "utf8"));
assert.equal(saved.pr, 42);
assert.equal(saved.phases.analyze.result, "done");

// The command line, with the feature from SPECIFY_FEATURE_DIRECTORY.
const cli = (...args) =>
  JSON.parse(
    execFileSync("node", [fileURLToPath(new URL("./flow.mjs", import.meta.url)), ...args], {
      cwd: root,
      env: { ...process.env, SPECIFY_FEATURE_DIRECTORY: dir },
      stdio: ["ignore", "pipe", "pipe"],
    }).toString(),
  );
assert.equal(cli("tasks-state").open.person[0], "T016");
assert.equal(cli("record", "merge", "skipped").phase, "merge");
assert.equal(cli("next").phase, "done");
assert.throws(() => cli("record", "merge", "maybe"), /unknown result/);

// Checkpoints, in a second feature. Each block is named by the OVEN-2 criterion it proves.
const feature = join(root, "specs", "002-dealer");
mkdirSync(feature, { recursive: true });
const put = (name, text) => writeFileSync(join(feature, name), text);
const run = () => next(feature, root);
const rowOf = (name, id) =>
  JSON.parse(readFileSync(join(feature, ".flow", "checkpoints", `${name}.json`), "utf8")).decisions.find((row) => row.id === id);
put("spec.md", "# Find a dealer\n\n## Clarifications\n\n- [C1] Search by ZIP.\n- [C2] No dealer near.\n");
assert.equal(run().phase, "plan");
assert.equal(loadState(feature).format, 2, "a feature begun with checkpoints carries the format");
record(feature, "clarify", "done");

// C1: a scope decision is a row with its fields, and spec.md gains no marker.
const spec = readFileSync(join(feature, "spec.md"), "utf8");
const q = (id, audience, extra = {}) => ({
  id,
  audience,
  question: `Question ${id}?`,
  recommended: "Yes",
  alternative: "No",
  ...extra,
});
addRow(feature, "product", q("A1", "po"));
addRow(feature, "product", q("Q2", "client"));
const a1 = rowOf("product", "A1");
assert.deepEqual(
  [a1.id, a1.audience, a1.question, a1.recommended, a1.alternative, a1.answer, a1.class],
  ["A1", "po", "Question A1?", "Yes", "No", null, "scope"],
  "C1",
);
assert.equal(readFileSync(join(feature, "spec.md"), "utf8"), spec, "C1: spec.md is untouched");
assert.throws(() => addRow(feature, "design", q("A1", "po")), /already exists/, "C1: ids are unique per feature");
assert.throws(() => addRow(feature, "product", { id: "Q9", audience: "po" }), /missing question/, "C1");

// C2: an open row holds with the checkpoint name and the open ids by audience, and no phase.
const hold = run();
assert.equal(hold.phase, "held", "C2");
assert.equal(hold.checkpoint, "product", "C2");
assert.deepEqual(hold.open, { po: ["A1"], client: ["Q2"] }, "C2");
assert.equal(hold.reason, undefined, "C2: names no next phase");
assert.equal(hold.cause, null, "C2: a checkpoint hold is not the held phase");
assert.equal(release(feature).released, false, "C2: release never lifts a checkpoint hold");
assert.deepEqual(loadState(feature).checkpoint, { name: "product", open: { po: ["A1"], client: ["Q2"] } }, "C2");
assert.deepEqual(status(join(root, "specs"), root)[1], { feature: "002-dealer", phase: "held", pr: null, cause: null, checkpoint: "product" });

// C11: an interim answer lifts the hold, and the row stays listed for the client.
answer(feature, "A1", { answer: "Yes", by: "po@bizstream.com", channel: "session" });
answer(feature, "Q2", { answer: "Yes", by: "po@bizstream.com", channel: "session", interim: true });
assert.equal(rowOf("product", "Q2").interim, true, "C11");

// C3: every row answered, the hold lifts and next names the next phase.
const lifted = run();
assert.equal(lifted.phase, "plan", "C3");
assert.deepEqual(lifted.checkpoints[0].interim, { client: ["Q2"] }, "C11: still listed for the client");
assert.equal(loadState(feature).checkpoint, null, "C3");
addRow(feature, "design", q("D0", "plan-authors"));
assert.equal(run().phase, "plan", "an open design row holds only implement and later");
answer(feature, "D0", { answer: "Yes", by: "dev@bizstream.com", channel: "session" });

// C11: the client's answer replaces the interim one with no --replace; an answer for good needs one.
put("plan.md", "# Plan\n");
record(feature, "plan", "done");
answer(feature, "Q2", { answer: "Yes", by: "dana@client.example", channel: "jira" });
assert.equal(rowOf("product", "Q2").interim, undefined, "C11");
assert.throws(() => answer(feature, "Q2", { answer: "No", by: "x", channel: "session" }), /--replace/);
assert.throws(() => answer(feature, "Q7", { answer: "No", by: "x", channel: "session" }), /no row Q7/);

// C9: an answer the client already gave, relayed by the person.
addRow(feature, "product", q("Q3", "client", { phase: "plan" }));
answer(feature, "Q3", { answer: "Yes", by: "dana@client.example", relayed_by: "po@bizstream.com", channel: "relayed" });
const q3 = rowOf("product", "Q3");
assert.deepEqual([q3.by, q3.relayed_by, q3.channel], ["dana@client.example", "po@bizstream.com", "relayed"], "C9");

// An answer that differs from the one the draft used sends back the phase that applies it.
answer(feature, "Q3", { answer: "No", by: "dana@client.example", channel: "jira", replace: "client changed it" });
assert.match(run().reason, /answer to Q3 changed after plan.md/);
assert.equal(run().phase, "plan", "a plan row never sends clarify back");
record(feature, "plan", "done");
assert.equal(run().phase, "tasks");

// C10: an answer from meeting notes keeps its source and the client's words.
addRow(feature, "product", q("Q4", "client"));
answer(feature, "Q4", {
  answer: "No",
  by: "dana@client.example",
  relayed_by: "po@bizstream.com",
  channel: "notes",
  source: "notes/2026-10-27.md",
  reply: "Let's keep it US only for now.",
});
assert.deepEqual([rowOf("product", "Q4").source, rowOf("product", "Q4").reply], ["notes/2026-10-27.md", "Let's keep it US only for now."], "C10");
const redo = run();
assert.equal(redo.phase, "clarify", "a spec row sends clarify back first");
record(feature, "clarify", "done");
assert.equal(run().phase, "plan", "then plan, whose file predates the answer");
record(feature, "plan", "done");

// C11: the client's answer, the same as the interim one the draft already took, sends nothing back.
addRow(feature, "product", q("Q6", "client"));
answer(feature, "Q6", { answer: "No", by: "po@bizstream.com", channel: "session", interim: true });
assert.equal(run().phase, "clarify", "C11: the interim answer changed the draft");
record(feature, "clarify", "done");
record(feature, "plan", "done");
answer(feature, "Q6", { answer: "No", by: "dana@client.example", channel: "jira" });
assert.equal(run().phase, "tasks", "C11");

// C8: an open client row with affects holds nothing; only the tasks that name its ids wait.
addRow(feature, "design", q("D1", "po"));
assert.deepEqual(cli("route", feature, "D1", "--audience", "client", "--affects", "C2").affects, ["C2"], "C8: Ask the client");
assert.equal(rowOf("design", "D1").audience, "client", "C8");
put("tasks.md", "- [ ] T1 [C1] Search\n- [ ] T2 [P] [C2] Empty state\n- [ ] T3 [WAITS:T2] Copy\n");
record(feature, "tasks", "done");
record(feature, "analyze", "done");
const bake = run();
assert.equal(bake.phase, "implement", "C8: the row holds no phase");
assert.deepEqual(bake.tasks.open, { runnable: ["T1"], waits: ["T2", "T3"], person: [], gated: [] }, "C8");
assert.deepEqual(bake.tasks.edges[0], { row: "T2", on: ["D1"] }, "C8");
assert.deepEqual(cli("tasks-state", feature).open.waits, ["T2", "T3"], "C8: the command line agrees");

// A design row with no affects holds implement.
addRow(feature, "design", q("D2", "plan-authors"));
assert.equal(run().checkpoint, "design");
answer(feature, "D2", { answer: "Yes", by: "dev@bizstream.com", channel: "session" });
assert.equal(run().phase, "implement");

// C7: an old GATED marker is an open product row that holds nothing; a feature with
// no checkpoint files passes the new guards.
put("spec.md", `${readFileSync(join(feature, "spec.md"), "utf8")}[NEEDS CLARIFICATION: GATED — sales: price the tier]\n`);
assert.deepEqual(markerRows(feature)[0], {
  id: "GATED-1",
  audience: "sales",
  question: "price the tier",
  marker: "spec.md",
  answer: null,
}, "C7");
assert.deepEqual(checkpoints(feature)[0].open.sales, ["GATED-1"], "C7");
assert.equal(run().phase, "implement", "C7: the marker holds nothing");
assert.deepEqual(checkpoints(dir).map((entry) => entry.holding), [[]], "C7: the first feature has only its old marker");
assert.equal(next(dir, root).phase, "done", "C7: and its guards pass");

// The command line: add from stdin, answer, and exit 1 on a refusal or a broken file.
const flow = fileURLToPath(new URL("./flow.mjs", import.meta.url));
const exec = (args, input) =>
  execFileSync("node", [flow, ...args], { cwd: root, input, stdio: ["pipe", "pipe", "pipe"] }).toString();
assert.equal(JSON.parse(exec(["add", feature, "product"], JSON.stringify(q("Q5", "po")))).row, "Q5");
assert.equal(JSON.parse(exec(["answer", feature, "Q5", "--answer", "Yes", "--by", "po", "--channel", "session"])).row, "Q5");
const exitOf = (args) => {
  try {
    exec(args);
    return 0;
  } catch (error) {
    return error.status;
  }
};
assert.equal(exitOf(["answer", feature, "Q5", "--answer", "No", "--by", "po", "--channel", "session"]), 1, "answered: refused");
assert.equal(exitOf(["answer", feature, "Q8", "--answer", "No", "--by", "po", "--channel", "session"]), 1, "unknown id: refused");
writeFileSync(join(feature, ".flow", "checkpoints", "design.json"), "{ not json");
assert.equal(exitOf(["next", feature]), 1, "a broken checkpoint never reads as answered");
assert.equal(exitOf(["frobnicate"]), 2, "usage");

rmSync(root, { recursive: true, force: true });
console.log("flow selftest: ok");
