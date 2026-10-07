import { createHash } from "node:crypto";
import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

// claude plugin eval passes only EVAL_* variables to a run. process.env stores
// an assigned undefined as the string "undefined", so assign only a real key.
if (!process.env.JEV_API_KEY && process.env.EVAL_JEV_API_KEY) process.env.JEV_API_KEY = process.env.EVAL_JEV_API_KEY;

export const MODEL = "jev-1.13.0";

// The plugin bank ships beside this module. A project bank sits in the working
// directory, and its entries override plugin entries that share an id.
export const PLUGIN_BANK = fileURLToPath(new URL("./questions.json", import.meta.url));
export const PROJECT_BANK = ".claude/jev/questions.json";
export const PLUGIN_CASES = fileURLToPath(new URL("./calibration.jsonl", import.meta.url));
export const PROJECT_CASES = ".claude/jev/calibration.jsonl";

const ENDPOINT = "https://api.typesafe.ai/v1/systemone";
const RETRY_STATUS = new Set([429, 529]);
const MAX_ATTEMPTS = 5;

export const SKIPPED = "skipped";
export const PASS = "pass";
export const FAIL = "fail";
export const REVIEW = "review";

const HASHED_FIELDS = ["primitive", "text", "criteria", "band", "failWhen"];

export function questionHash(question) {
  const semantic = {};
  for (const field of HASHED_FIELDS) {
    if (question[field] !== undefined) semantic[field] = question[field];
  }
  return createHash("sha256")
    .update(`${MODEL}\u0000${JSON.stringify(semantic)}`)
    .digest("hex")
    .slice(0, 16);
}

export function validateBank(bank) {
  const problems = [];
  const seen = new Set();
  if (bank.model !== MODEL) {
    problems.push(`bank pins ${bank.model}, core pins ${MODEL}: calibrate before bumping`);
  }
  for (const question of bank.questions ?? []) {
    const where = question.id ?? "<no id>";
    if (!question.id) problems.push("a question has no id");
    if (seen.has(question.id)) problems.push(`${where}: duplicate id`);
    seen.add(question.id);
    if (!question.text) problems.push(`${where}: no text`);

    switch (question.primitive) {
      case "noul": {
        if (!["true", "false"].includes(question.failWhen)) {
          problems.push(`${where}: a noul needs failWhen true or false`);
        }
        const band = question.band;
        if (!Array.isArray(band) || band.length !== 2 || !(band[0] < band[1])) {
          problems.push(`${where}: band must be [low, high] with low below high`);
        }
        const criteria = question.criteria;
        if (criteria !== undefined) {
          if (criteria.true === undefined || criteria.false === undefined) {
            problems.push(`${where}: noul criteria needs both a true and a false description`);
          }
        }
        break;
      }
      case "choice": {
        const options = Object.keys(question.criteria ?? {});
        if (options.length < 2) problems.push(`${where}: a choice needs two or more options`);
        if (options.length > 255) problems.push(`${where}: a choice takes at most 255 options`);
        break;
      }
      case "score": {
        const levels = question.criteria;
        if (!Array.isArray(levels) || levels.length < 2 || levels.length > 10) {
          problems.push(`${where}: a score needs an ordered array of 2 to 10 level descriptions`);
        }
        break;
      }
      default:
        problems.push(`${where}: primitive must be noul, choice or score`);
    }

    for (const [index, entry] of (question.floor ?? []).entries()) {
      if (typeof entry.then !== "string") problems.push(`${where}: floor ${index} needs a then`);
      try {
        new RegExp(entry.match, entry.flags ?? "i");
      } catch {
        problems.push(`${where}: floor ${index} match is not a valid regex`);
      }
    }
    if (question.then !== undefined && question.primitive !== "noul") {
      problems.push(`${where}: then maps pass and fail, so only a noul takes it`);
    }

    if (question.hash && question.hash !== questionHash(question)) {
      problems.push(`${where}: hash is stale, so its recorded verdicts no longer apply`);
    }
  }
  return problems;
}

export function loadBank(paths = [PLUGIN_BANK, PROJECT_BANK]) {
  const merged = new Map();
  for (const path of paths) {
    if (!existsSync(path)) continue;
    const bank = JSON.parse(readFileSync(path, "utf8"));
    const problems = validateBank(bank);
    if (problems.length > 0) {
      throw new Error(`bank ${path} is invalid:\n  ${problems.join("\n  ")}`);
    }
    for (const question of bank.questions) merged.set(question.id, question);
  }
  return { model: MODEL, questions: [...merged.values()] };
}

export function findQuestion(id, bank = loadBank()) {
  const question = bank.questions.find((entry) => entry.id === id);
  if (!question) throw new Error(`the bank holds no question ${id}`);
  return question;
}

// A noul answer carries no confidence field, so the band is the only signal
// that separates a decided answer from one a person has to read.
export function verdict(probability, question) {
  const [low, high] = question.band;
  if (probability >= low && probability <= high) return REVIEW;
  const isTrue = probability > high;
  const failsOnTrue = question.failWhen === "true";
  return isTrue === failsOnTrue ? FAIL : PASS;
}

// A wrong pass hides a defect and a wrong review costs one read, so the worse
// verdict always wins.
export function worst(verdicts) {
  if (verdicts.includes(FAIL)) return FAIL;
  if (verdicts.includes(REVIEW)) return REVIEW;
  return verdicts.length > 0 ? PASS : SKIPPED;
}

function toApiQuestion(question) {
  const body = { type: question.primitive, instructions: question.text };
  if (question.criteria !== undefined) body.criteria = question.criteria;
  return body;
}

async function post(payload) {
  for (let attempt = 0; ; attempt += 1) {
    const response = await fetch(ENDPOINT, {
      method: "POST",
      headers: {
        authorization: `Bearer ${process.env.JEV_API_KEY}`,
        "content-type": "application/json",
      },
      body: JSON.stringify(payload),
    });
    if (response.ok) return response.json();
    const detail = await response.text();
    if (!RETRY_STATUS.has(response.status) || attempt >= MAX_ATTEMPTS - 1) {
      throw new Error(`jev ${response.status}: ${detail}`);
    }
    await new Promise((wake) => setTimeout(wake, 2 ** attempt * 500));
  }
}

// The question key is chosen here and echoed back on the answer, so nothing
// depends on the order the questions were sent in.
async function evaluate(state, questions) {
  return post({
    state,
    model: MODEL,
    questions: Object.fromEntries(questions.map((q) => [q.id, toApiQuestion(q)])),
  });
}

export async function ask(state, questions, options = {}) {
  const { required = false, samples = 1, transport = evaluate } = options;
  if (!process.env.JEV_API_KEY) {
    if (required) throw new Error("JEV_API_KEY is not set and this gate requires it");
    process.stderr.write("jev: skipped, JEV_API_KEY is not set\n");
    return null;
  }
  const rounds = [];
  let inputTokens = 0;
  for (let round = 0; round < samples; round += 1) {
    const body = await transport(state, questions);
    // Every recorded verdict is pinned to one model version, so a served
    // version that differs makes the whole bank meaningless until it is
    // recalibrated.
    if (body.model !== MODEL) {
      throw new Error(`jev served ${body.model} against the pin ${MODEL}: recalibrate first`);
    }
    rounds.push(body.answers);
    inputTokens += body.usage?.input_tokens ?? 0;
  }
  return questions.map((question) => {
    const answers = rounds.map((round) => round[question.id]);
    // A choice answer is its option, so a split sample is a review in the same way.
    const verdicts =
      question.primitive === "choice"
        ? answers.map((answer) => answer.choice)
        : answers.map((answer) => verdict(answer.noul, question));
    const agreed = new Set(verdicts).size === 1;
    return {
      id: question.id,
      hash: questionHash(question),
      verdict: agreed ? verdicts[0] : REVIEW,
      agreed,
      samples: verdicts,
      confidence: Math.min(...answers.map((answer) => answer.confidence ?? 1)),
      inputTokens,
    };
  });
}
