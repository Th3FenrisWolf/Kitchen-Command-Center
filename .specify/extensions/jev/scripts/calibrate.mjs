import { existsSync, readFileSync, writeFileSync } from "node:fs";
import {
  ask,
  loadBank,
  questionHash,
  validateBank,
  MODEL,
  PLUGIN_BANK,
  PROJECT_BANK,
  PLUGIN_CASES,
  PROJECT_CASES,
} from "./core.mjs";

const BAR = 20;

function stamp(path) {
  const bank = JSON.parse(readFileSync(path, "utf8"));
  for (const question of bank.questions) question.hash = questionHash(question);
  writeFileSync(path, `${JSON.stringify(bank, null, 2)}\n`);
  console.log(`stamped ${bank.questions.length} questions in ${path} against ${MODEL}`);
}

function checkBanks() {
  let code = 0;
  for (const path of [PLUGIN_BANK, PROJECT_BANK]) {
    if (!existsSync(path)) continue;
    const bank = JSON.parse(readFileSync(path, "utf8"));
    const problems = validateBank(bank);
    if (problems.length > 0) {
      console.error(`${path} invalid:\n  ${problems.join("\n  ")}`);
      code = 1;
      continue;
    }
    console.log(`${path}: ${bank.questions.length} questions on ${MODEL}`);
  }
  return code;
}

function readCases() {
  return [PLUGIN_CASES, PROJECT_CASES]
    .filter((path) => existsSync(path))
    .flatMap((path) => readFileSync(path, "utf8").split("\n"))
    .filter((line) => line.trim().length > 0)
    .map((line) => JSON.parse(line));
}

// Needs JEV_API_KEY. Each case carries a state and the verdict a careful
// reviewer gives it, so a question that disagrees gets rewritten or dropped.
async function agreement() {
  const bank = loadBank();
  const byId = new Map(bank.questions.map((question) => [question.id, question]));
  const cases = readCases();

  const tally = new Map();
  for (const item of cases) {
    const question = byId.get(item.question);
    if (!question) {
      console.error(`case references unknown question ${item.question}`);
      return 1;
    }
    const answers = await ask(item.state, [question], { samples: question.samples ?? 1 });
    if (answers === null) {
      console.log("\nagreement needs JEV_API_KEY, skipped");
      return 0;
    }
    const record = tally.get(item.question) ?? { hit: 0, miss: 0, review: 0, total: 0, cases: [] };
    record.total += 1;
    if (answers[0].verdict === "review") record.review += 1;
    else if (answers[0].verdict === item.expected) record.hit += 1;
    else {
      record.miss += 1;
      record.cases.push({ expected: item.expected, got: answers[0].verdict, state: item.state });
    }
    tally.set(item.question, record);
  }

  // A miss is a confident wrong answer, and it is the only outcome that passes
  // a defect through a gate. A review means the band asked for a reader.
  console.log(`\nagreement over ${cases.length} cases:`);
  let misses = 0;
  for (const [id, record] of [...tally].sort()) {
    misses += record.miss;
    const decided = record.hit + record.miss;
    const rate = decided === 0 ? "n/a" : `${Math.round((record.hit / decided) * 100)}%`;
    const flag = record.total < BAR ? "  (too few cases to trust)" : "";
    console.log(
      `  ${id}: ${record.hit} hit, ${record.miss} miss, ${record.review} review` +
        `, ${rate} of decided${flag}`,
    );
    for (const item of record.cases) {
      console.log(`      expected ${item.expected}, got ${item.got}`);
      console.log(`      ${item.state.replace(/\n/g, " | ").slice(0, 160)}`);
    }
  }
  console.log(`\n${misses} confident wrong answers over ${cases.length} cases`);
  if (misses > 0) {
    console.log("Read each case above before you change a question. The label may be wrong.");
  }

  const unfit = [];
  for (const question of bank.questions.filter((entry) => entry.blocking)) {
    const record = tally.get(question.id);
    if (!record || record.total < BAR) {
      unfit.push(`${question.id}: blocking on ${record?.total ?? 0} cases, the bar is ${BAR}`);
    } else if (record.miss > 0) {
      unfit.push(`${question.id}: blocking with ${record.miss} confident wrong answers`);
    }
  }
  if (unfit.length > 0) {
    console.error(`\nblocking questions below the bar:\n  ${unfit.join("\n  ")}`);
    return 1;
  }
  const blocking = bank.questions.filter((entry) => entry.blocking).length;
  console.log(`${blocking} of ${bank.questions.length} questions are fit to block`);
  return 0;
}

// Offline, the bar still holds on case counts, so a blocking question with too
// few cases fails before anyone spends a call on it.
function caseCounts() {
  const counts = new Map();
  for (const item of readCases()) counts.set(item.question, (counts.get(item.question) ?? 0) + 1);
  const short = loadBank()
    .questions.filter((question) => question.blocking && (counts.get(question.id) ?? 0) < BAR)
    .map((question) => `${question.id}: ${counts.get(question.id) ?? 0} cases, the bar is ${BAR}`);
  if (short.length > 0) {
    console.error(`blocking questions below the bar:\n  ${short.join("\n  ")}`);
    return 1;
  }
  return 0;
}

const flags = process.argv.slice(2);
const stampAt = flags.indexOf("--stamp");
if (stampAt >= 0) {
  const path = flags[stampAt + 1];
  if (!path) {
    console.error("usage: node <jev>/scripts/calibrate.mjs --stamp <bank path>");
    process.exit(2);
  }
  stamp(path);
} else {
  let code = checkBanks();
  if (code === 0) code = caseCounts();
  if (code === 0 && !flags.includes("--offline")) code = await agreement();
  process.exit(code);
}
