// The bookkeeping of speckit.flow.run: which phase is next, what each phase
// recorded, and which tasks.md rows can still run. Facts only; no model call.
//
//   node flow.mjs next <feature>
//   node flow.mjs record <feature> <phase> <done|skipped|blocked|failed> [--pr <n>] [--note <text>]
//   node flow.mjs release <feature>
//   node flow.mjs status [specs-dir]
//   node flow.mjs tasks-state <feature>
//   node flow.mjs add <feature> <product|design> [row JSON, else stdin]
//   node flow.mjs answer <feature> <id> --answer <text> --by <who> --channel <how>
//       [--reply <text>] [--relayed-by <who>] [--source <path>] [--interim] [--replace <reason>]
//   node flow.mjs route <feature> <id> --audience <who> [--affects <id,id>]
//   node flow.mjs checkpoints <feature>
//
// <feature> is a feature directory or a path inside one. Without it, the
// commands read SPECIFY_FEATURE_DIRECTORY. Every command prints one JSON value.
// A refusal (an unknown row, an answered row, a checkpoint file that is not
// JSON) exits 1; any other error exits 2.
import { existsSync, mkdirSync, readdirSync, readFileSync, renameSync, statSync, writeFileSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

export const PHASES = [
  "specify",
  "clarify",
  "plan",
  "assumption-review",
  "tasks",
  "analyze",
  "implement",
  "qa-audit",
  "qa-walk",
  "merge",
];
export const DONE = "done";
export const HELD = "held";
const RESULTS = ["done", "skipped", "blocked", "failed"];
// A second blocked or failed result in one phase is a stop.
const MAX_ATTEMPTS = 2;

// Each checkpoint holds the phase it blocks while a row waits for a person.
export const CHECKPOINTS = { product: "plan", design: "implement" };
// The phase that applies a row's answer, when the row names none.
const APPLIES = { product: "clarify", design: "plan" };
// The phases a changed answer sends back, each with the artifact it rewrites.
const REDO = { clarify: "spec.md", plan: "plan.md", tasks: "tasks.md" };
const ROW_FIELDS = ["id", "audience", "question", "recommended", "alternative"];
// A feature whose state.json predates checkpoints has no format and reads as 1.
export const FORMAT = 2;

const STATE_FILE = join(".flow", "state.json");
const CHECKPOINT_DIR = join(".flow", "checkpoints");
const FLOW_CONFIG = join(".specify", "extensions", "flow", "flow-config.yml");
const QA_AUDIT_COMMAND = join(".specify", "extensions", "qa", "commands", "speckit.qa.audit.md");
const QA_EXTENSION = join(".specify", "extensions", "qa");
// The unfilled header lines of the core spec template.
const SPEC_PLACEHOLDER = /\[(FEATURE NAME|###-feature-name|DATE)\]/;

const read = (path) => (existsSync(path) ? readFileSync(path, "utf8") : null);

export class Refusal extends Error {}

function writeJson(path, value) {
  mkdirSync(dirname(path), { recursive: true });
  // Write and rename, so a reader never sees half a file.
  writeFileSync(`${path}.tmp`, `${JSON.stringify(value, null, 2)}\n`);
  renameSync(`${path}.tmp`, path);
}

export function featureDir(arg = process.env.SPECIFY_FEATURE_DIRECTORY) {
  if (!arg) throw new Error("name a feature directory, or set SPECIFY_FEATURE_DIRECTORY");
  let dir = resolve(arg);
  if (existsSync(dir) && statSync(dir).isFile()) dir = dirname(dir);
  // A path inside a feature walks up to the directory that holds spec.md.
  for (let probe = dir; ; probe = dirname(probe)) {
    if (existsSync(join(probe, "spec.md"))) return probe;
    if (dirname(probe) === probe) return dir;
  }
}

// Counts every [NEEDS CLARIFICATION marker in the feature's artifacts.
export function markers(dir) {
  const count = { open: 0, gated: 0 };
  for (const name of ["spec.md", "plan.md", "tasks.md"]) {
    for (const hit of (read(join(dir, name)) ?? "").matchAll(/\[NEEDS CLARIFICATION:?\s*(GATED)?/g)) {
      count[hit[1] ? "gated" : "open"] += 1;
    }
  }
  return count;
}

// Assumption ids in spec.md that no later Reviewed: line covers.
export function unreviewedAssumptions(specText) {
  const ids = [];
  for (const line of (specText ?? "").split("\n")) {
    const id = line.match(/^\s*- (A\d+):/);
    if (id) ids.push(id[1]);
    else if (/^\s*Reviewed:/.test(line)) ids.length = 0;
  }
  return ids;
}

// One row state per tasks.md row, as R2 of the flow strategy keeps it:
// done, person ([PERSON]), gated (GATED), waits (on a row that is not done),
// or runnable. A row waits on the ids in a [WAITS:Tnnn] tag, or in a clause
// that starts "Only if", "Depends on" or "Blocked by". It also waits on an
// open checkpoint row that names an id in one of its tags, such as [C1]:
// `affects` maps each such id to the checkpoint rows.
export function tasksState(text, affects = {}) {
  const openRows = new Set(Object.values(affects).flat());
  const rows = [];
  for (const line of (text ?? "").split("\n")) {
    const row = line.match(/^\s*- \[([ xX])\] (T\d+)\b(.*)$/);
    if (!row) continue;
    const [, mark, id, rest] = row;
    const on = new Set();
    for (const tag of rest.matchAll(/\[([^\]]+)\]/g)) {
      for (const ref of tag[1].split(/[\s,]+/)) for (const rowId of affects[ref] ?? []) on.add(rowId);
    }
    for (const tag of rest.matchAll(/\[WAITS:\s*([^\]]+)\]/gi)) {
      for (const ref of tag[1].matchAll(/T\d+/g)) on.add(ref[0]);
    }
    for (const clause of rest.matchAll(/\b(?:only if|depends on|blocked by)\b([^.;]*)/gi)) {
      for (const ref of clause[1].matchAll(/\bT\d+\b/g)) on.add(ref[0]);
    }
    on.delete(id);
    let tag = "open";
    if (mark !== " ") tag = "done";
    else if (/\[PERSON\]/i.test(rest)) tag = "person";
    else if (/\bGATED\b/.test(rest)) tag = "gated";
    rows.push({ id, tag, on: [...on] });
  }

  const byId = new Map(rows.map((row) => [row.id, row]));
  // A row is stuck when this session can never run it: a person or gated row,
  // or an open row that waits, at any depth, on a stuck row or a checkpoint row.
  const stuck = new Map();
  const isStuck = (row, seen = new Set()) => {
    if (stuck.has(row.id)) return stuck.get(row.id);
    if (row.tag === "done") return false;
    if (seen.has(row.id)) return false;
    seen.add(row.id);
    const result =
      row.tag === "person" ||
      row.tag === "gated" ||
      row.on.some((ref) => openRows.has(ref) || (byId.has(ref) && isStuck(byId.get(ref), seen)));
    stuck.set(row.id, result);
    return result;
  };

  const open = { runnable: [], waits: [], person: [], gated: [] };
  const edges = [];
  for (const row of rows) {
    if (row.tag === "done") continue;
    const pending = row.on.filter((ref) => byId.get(ref)?.tag !== "done");
    if (pending.length) edges.push({ row: row.id, on: pending });
    if (row.tag === "person" || row.tag === "gated") open[row.tag].push(row.id);
    else if (isStuck(row)) open.waits.push(row.id);
    else open.runnable.push(row.id);
  }
  return {
    total: rows.length,
    done: rows.filter((row) => row.tag === "done").length,
    open,
    edges,
  };
}

const checkpointPath = (dir, name) => join(dir, CHECKPOINT_DIR, `${name}.json`);

function readCheckpoint(dir, name) {
  const path = checkpointPath(dir, name);
  const text = read(path);
  if (text === null) return null;
  try {
    return JSON.parse(text);
  } catch (error) {
    // Never read a broken file as answered.
    throw new Refusal(`${path} is not valid JSON: ${error.message}`);
  }
}

// GATED markers from runs before checkpoints, listed as open product rows.
// They hold nothing, as before, until a phase moves them into the file.
export function markerRows(dir) {
  const rows = [];
  for (const name of ["spec.md", "plan.md", "tasks.md"]) {
    const text = read(join(dir, name)) ?? "";
    for (const hit of text.matchAll(/\[NEEDS CLARIFICATION:?\s*GATED\s*[—–-]+\s*([^:\]]+):\s*([^\]]*)\]/g)) {
      rows.push({ id: `GATED-${rows.length + 1}`, audience: hit[1].trim(), question: hit[2].trim(), marker: name, answer: null });
    }
  }
  return rows;
}

function findRow(dir, id) {
  for (const name of Object.keys(CHECKPOINTS)) {
    const file = readCheckpoint(dir, name);
    const row = file?.decisions?.find((entry) => entry.id === id);
    if (row) return { name, file, row };
  }
  return null;
}

// Each checkpoint that has rows, with its open and interim ids by audience.
// An open row with no `affects` holds the phase the checkpoint blocks; an open
// row with `affects` makes only the tasks that name those ids wait. An interim
// answer lifts the hold, and the row stays listed until its answer comes.
export function checkpoints(dir) {
  const result = [];
  for (const [name, blocks] of Object.entries(CHECKPOINTS)) {
    const rows = [...(readCheckpoint(dir, name)?.decisions ?? []), ...(name === "product" ? markerRows(dir) : [])];
    if (!rows.length) continue;
    const entry = { name, blocks, open: {}, interim: {}, holding: [], affects: {} };
    for (const row of rows) {
      if (row.answer != null && !row.interim) continue;
      const list = row.answer == null ? entry.open : entry.interim;
      (list[row.audience] ??= []).push(row.id);
      if (row.answer != null || row.marker) continue;
      if (row.affects?.length) for (const ref of row.affects) (entry.affects[ref] ??= []).push(row.id);
      else entry.holding.push(row.id);
    }
    result.push(entry);
  }
  return result;
}

// Appends one open row; refuses a row with a missing field or an id the
// feature already has.
export function addRow(dir, name, row) {
  if (!(name in CHECKPOINTS)) throw new Error(`unknown checkpoint ${name}: one of ${Object.keys(CHECKPOINTS).join(", ")}`);
  const missing = ROW_FIELDS.filter((field) => !row?.[field]);
  if (missing.length) throw new Refusal(`row is missing ${missing.join(", ")}`);
  if (findRow(dir, row.id)) throw new Refusal(`row ${row.id} already exists`);
  if (row.phase && !(row.phase in REDO)) throw new Refusal(`row phase ${row.phase}: one of ${Object.keys(REDO).join(", ")}`);
  const file = readCheckpoint(dir, name) ?? { checkpoint: name, blocks: CHECKPOINTS[name], decisions: [] };
  file.decisions.push({
    class: "scope",
    phase: APPLIES[name],
    affects: [],
    ...row,
    answer: null,
    by: null,
    at: null,
    channel: null,
  });
  writeJson(checkpointPath(dir, name), file);
  return { feature: dir, checkpoint: name, row: row.id };
}

// Gives an open row a new audience and, when named, the ids that wait for it.
export function route(dir, id, { audience, affects } = {}) {
  if (!audience) throw new Error("route needs --audience");
  const found = findRow(dir, id);
  if (!found) throw new Refusal(`no row ${id} in ${join(dir, CHECKPOINT_DIR)}`);
  const { name, file, row } = found;
  if (row.answer != null && !row.interim) throw new Refusal(`row ${id} is answered`);
  row.audience = audience;
  if (affects) row.affects = affects;
  writeJson(checkpointPath(dir, name), file);
  return { feature: dir, checkpoint: name, row: id, audience, affects: row.affects };
}

// Writes an answer into its row. A row answered for good changes only with
// replace and a reason; an interim answer gives way to the next answer.
export function answer(dir, id, { answer: text, by, channel, reply, relayed_by, source, interim, replace } = {}) {
  if (!text || !by || !channel) throw new Error("answer needs --answer, --by and --channel");
  const found = findRow(dir, id);
  if (!found) throw new Refusal(`no row ${id} in ${join(dir, CHECKPOINT_DIR)}`);
  const { name, file, row } = found;
  if (row.answer != null && !row.interim && !replace) {
    throw new Refusal(`row ${id} is answered; pass --replace <reason> to change it`);
  }
  Object.assign(row, { answer: text, by, at: new Date().toISOString(), channel });
  for (const [key, value] of Object.entries({ reply, relayed_by, source, replaced: replace })) {
    if (value) row[key] = value;
  }
  if (interim) row.interim = true;
  else delete row.interim;
  writeJson(checkpointPath(dir, name), file);
  return { feature: dir, checkpoint: name, row: id, interim: Boolean(interim) };
}

// The answered rows that a phase applies: its own and every earlier phase's.
function answeredFor(dir, phase) {
  const rows = [];
  for (const name of Object.keys(CHECKPOINTS)) {
    for (const row of readCheckpoint(dir, name)?.decisions ?? []) {
      if (row.answer != null && PHASES.indexOf(row.phase ?? APPLIES[name]) <= PHASES.indexOf(phase)) rows.push(row);
    }
  }
  return rows;
}

export function loadState(dir) {
  const text = read(join(dir, STATE_FILE));
  const state = text ? JSON.parse(text) : {};
  return { format: text ? 1 : FORMAT, phase: null, attempt: 0, pr: null, cause: null, checkpoint: null, phases: {}, ...state };
}

export function saveState(dir, state) {
  writeJson(join(dir, STATE_FILE), { ...state, updated: new Date().toISOString() });
}

export function publishMode(root = process.cwd()) {
  const mode = (read(join(root, FLOW_CONFIG)) ?? "").match(/^publish:\s*["']?(\w+)/m)?.[1];
  return mode === "auto" ? "auto" : "ask";
}

const recorded = (state, phase) => ["done", "skipped"].includes(state.phases[phase]?.result);

// The first phase whose exit guard does not hold, with the fact that decided it.
export function nextPhase(dir, state, root = process.cwd()) {
  const spec = read(join(dir, "spec.md"));
  const tasks = read(join(dir, "tasks.md"));
  const facts = { markers: markers(dir), checkpoints: checkpoints(dir) };
  // A phase runs again when an answer differs from the one its artifact was
  // drafted on: the answer it saw when it was last done, else the recommended one.
  const stale = (phase) => {
    if (!existsSync(join(dir, REDO[phase]))) return null;
    const drafted = state.phases[phase]?.answers ?? {};
    const ids = answeredFor(dir, phase)
      .filter((row) => row.answer !== (drafted[row.id] ?? row.recommended))
      .map((row) => row.id);
    return ids.length ? `answer to ${ids.join(", ")} changed after ${REDO[phase]} was written` : null;
  };
  const guards = {
    specify: () => (spec && !SPEC_PLACEHOLDER.test(spec) ? null : "spec.md is missing or unfilled"),
    clarify: () => {
      const redo = stale("clarify");
      if (redo) return redo;
      if (facts.markers.open) return `${facts.markers.open} open [NEEDS CLARIFICATION] marker(s)`;
      return /^## (Clarifications|Assumptions)\b/m.test(spec) || recorded(state, "clarify")
        ? null
        : "no clarify pass recorded";
    },
    plan: () => (existsSync(join(dir, "plan.md")) ? stale("plan") : "plan.md is missing"),
    "assumption-review": () => {
      const ids = unreviewedAssumptions(spec);
      facts.unreviewed = ids;
      return ids.length ? `${ids.length} unreviewed assumption(s)` : null;
    },
    tasks: () => (tasks ? stale("tasks") : "tasks.md is missing"),
    analyze: () => (recorded(state, "analyze") ? null : "analyze has not run"),
    implement: () => {
      facts.tasks = tasksState(tasks, affectsOf(facts.checkpoints));
      if (facts.tasks.open.runnable.length) return `${facts.tasks.open.runnable.length} runnable row(s) open`;
      return state.pr || recorded(state, "implement") ? null : "no pull request recorded";
    },
    "qa-audit": () =>
      existsSync(join(dir, "qa-audit-results.md")) ||
      !existsSync(join(root, QA_AUDIT_COMMAND)) ||
      recorded(state, "qa-audit")
        ? null
        : "qa-audit-results.md is missing",
    "qa-walk": () =>
      existsSync(join(dir, "qa-walk.sh")) ||
      existsSync(join(dir, "qa-walk.ps1")) ||
      !existsSync(join(root, QA_EXTENSION)) ||
      recorded(state, "qa-walk")
        ? null
        : "the QA wizard is missing",
    merge: () => (publishMode(root) === "ask" || recorded(state, "merge") ? null : "auto-merge is not enabled"),
  };
  for (const phase of PHASES) {
    const reason = guards[phase]();
    if (reason) return { phase, reason, facts };
  }
  return { phase: DONE, reason: "every phase is done", facts };
}

const affectsOf = (list) => {
  const merged = {};
  for (const entry of list) {
    for (const [ref, ids] of Object.entries(entry.affects)) (merged[ref] ??= []).push(...ids);
  }
  return merged;
};

// The first checkpoint that holds a row and blocks this phase or an earlier one.
const holdFor = (list, phase) => {
  const at = phase === DONE ? PHASES.length : PHASES.indexOf(phase);
  return list.find((entry) => entry.holding.length && PHASES.indexOf(entry.blocks) <= at) ?? null;
};

export function next(dir, root = process.cwd()) {
  const state = loadState(dir);
  if (state.phase === HELD) {
    return { feature: dir, phase: HELD, cause: state.cause, pr: state.pr };
  }
  const { phase, reason, facts } = nextPhase(dir, state, root);
  const hold = holdFor(facts.checkpoints, phase);
  // A checkpoint hold lives apart from the held phase: release never lifts it,
  // only the answers do.
  const checkpoint = hold && { name: hold.name, open: hold.open };
  if (phase !== state.phase || JSON.stringify(checkpoint) !== JSON.stringify(state.checkpoint)) {
    if (phase !== state.phase) state.attempt = state.phases[phase]?.attempt ?? 0;
    state.phase = phase;
    state.checkpoint = checkpoint;
    saveState(dir, state);
  }
  const headless = process.env.SPECKIT_HEADLESS === "1";
  if (hold) {
    return {
      feature: dir,
      phase: HELD,
      checkpoint: hold.name,
      blocks: hold.blocks,
      open: hold.open,
      interim: hold.interim,
      cause: null,
      pr: state.pr,
      headless,
    };
  }
  return { feature: dir, phase, reason, attempt: state.attempt, pr: state.pr, publish: publishMode(root), headless, ...facts };
}

export function record(dir, phase, result, { pr, note } = {}) {
  if (!PHASES.includes(phase)) throw new Error(`unknown phase ${phase}: one of ${PHASES.join(", ")}`);
  if (!RESULTS.includes(result)) throw new Error(`unknown result ${result}: one of ${RESULTS.join(", ")}`);
  const state = loadState(dir);
  const entry = state.phases[phase] ?? { attempt: 0 };
  if (result === "blocked" || result === "failed") entry.attempt += 1;
  else entry.attempt = 0;
  entry.result = result;
  // The answers the artifact now follows, so a later different one sends the phase back.
  if (result === "done" && phase in REDO) {
    entry.answers = Object.fromEntries(answeredFor(dir, phase).map((row) => [row.id, row.answer]));
  }
  if (note) entry.note = note;
  state.phases[phase] = entry;
  state.attempt = entry.attempt;
  if (pr) state.pr = Number(pr);
  let stop = false;
  if (entry.attempt >= MAX_ATTEMPTS) {
    state.phase = HELD;
    state.cause = `${phase} ${result} ${entry.attempt} times${note ? `: ${note}` : ""}`;
    stop = true;
  } else {
    state.phase = phase;
    state.cause = null;
  }
  saveState(dir, state);
  return { feature: dir, phase: state.phase, attempt: entry.attempt, pr: state.pr, cause: state.cause, stop };
}

// The owner's release of a held feature: the held phase gets two fresh tries.
export function release(dir) {
  const state = loadState(dir);
  if (state.phase !== HELD) return { feature: dir, phase: state.phase, released: false };
  for (const entry of Object.values(state.phases)) {
    if (entry.attempt >= MAX_ATTEMPTS) entry.attempt = 0;
  }
  state.phase = null;
  state.cause = null;
  saveState(dir, state);
  return { feature: dir, ...next(dir) };
}

export function status(specsDir = "specs", root = process.cwd()) {
  if (!existsSync(specsDir)) return [];
  return readdirSync(specsDir)
    .map((name) => join(specsDir, name))
    .filter((dir) => existsSync(join(dir, "spec.md")))
    .map((dir) => {
      const state = loadState(dir);
      if (state.phase === HELD) return { feature: basename(dir), phase: HELD, pr: state.pr, cause: state.cause };
      const { phase, facts } = nextPhase(dir, state, root);
      const hold = holdFor(facts.checkpoints, phase);
      return {
        feature: basename(dir),
        phase: hold ? HELD : phase,
        pr: state.pr,
        cause: state.cause,
        ...(hold && { checkpoint: hold.name }),
      };
    });
}

function option(args, name) {
  const at = args.indexOf(name);
  if (at === -1) return undefined;
  const [, value] = args.splice(at, 2);
  return value;
}

function flag(args, name) {
  const at = args.indexOf(name);
  if (at !== -1) args.splice(at, 1);
  return at !== -1;
}

function parseRow(text) {
  try {
    return JSON.parse(text);
  } catch (error) {
    throw new Refusal(`row is not valid JSON: ${error.message}`);
  }
}

function main(argv) {
  const args = [...argv];
  const pr = option(args, "--pr");
  const note = option(args, "--note");
  const reply = {
    answer: option(args, "--answer"),
    by: option(args, "--by"),
    channel: option(args, "--channel"),
    reply: option(args, "--reply"),
    relayed_by: option(args, "--relayed-by"),
    source: option(args, "--source"),
    replace: option(args, "--replace"),
    interim: flag(args, "--interim"),
  };
  const routing = { audience: option(args, "--audience"), affects: option(args, "--affects") };
  const [command, ...rest] = args;
  switch (command) {
    case "next":
      return next(featureDir(rest[0]));
    case "record": {
      // The feature argument may be left out in favor of SPECIFY_FEATURE_DIRECTORY.
      const named = rest.length === 3;
      const [phase, result] = named ? rest.slice(1) : rest;
      return record(featureDir(named ? rest[0] : undefined), phase, result, { pr, note });
    }
    case "release":
      return release(featureDir(rest[0]));
    case "status":
      return status(rest[0]);
    case "tasks-state": {
      const dir = featureDir(rest[0]);
      return { feature: dir, ...tasksState(read(join(dir, "tasks.md")), affectsOf(checkpoints(dir))) };
    }
    case "add": {
      // The feature may be left out; a checkpoint name in first place says so.
      const named = !(rest[0] in CHECKPOINTS);
      const [feature, name, row] = named ? rest : [undefined, ...rest];
      return addRow(featureDir(feature), name, parseRow(row ?? readFileSync(0, "utf8")));
    }
    case "answer": {
      const named = rest.length === 2;
      return answer(featureDir(named ? rest[0] : undefined), named ? rest[1] : rest[0], reply);
    }
    case "route": {
      const named = rest.length === 2;
      const affects = routing.affects?.split(",").map((ref) => ref.trim()).filter(Boolean);
      return route(featureDir(named ? rest[0] : undefined), named ? rest[1] : rest[0], { audience: routing.audience, affects });
    }
    case "checkpoints": {
      const dir = featureDir(rest[0]);
      return { feature: dir, checkpoints: checkpoints(dir) };
    }
    default:
      throw new Error(
        "usage: flow.mjs next|record|release|status|tasks-state|add|answer|route|checkpoints [feature] (see the header of flow.mjs)",
      );
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    process.stdout.write(`${JSON.stringify(main(process.argv.slice(2)))}\n`);
  } catch (error) {
    process.stderr.write(`flow.mjs: ${error.message}\n`);
    process.exit(error instanceof Refusal ? 1 : 2);
  }
}
