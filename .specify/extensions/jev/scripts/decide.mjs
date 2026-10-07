import { readFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { ask, findQuestion, REVIEW } from "./core.mjs";

// A switch statement whose default case asks Jev. The floor cases are code:
// an owner decision, an irreversible action or a trust boundary matches a
// pattern and returns before any probability exists. Jev answers only the
// residue, and every answer it is unsure of takes the fallback.

export async function decide(state, question, { samples, transport } = {}) {
  const text = typeof state === "string" ? state : JSON.stringify(state, null, 2);
  for (const entry of question.floor ?? []) {
    if (new RegExp(entry.match, entry.flags ?? "i").test(text)) {
      return { outcome: entry.then, via: "floor", reason: entry.match };
    }
  }

  // With no fallback declared, an unsure answer surfaces as review, so a
  // skip or a split sample never reads as a decision.
  const fallback = question.fallback ?? REVIEW;
  const answers = await ask(text, [question], {
    samples: samples ?? question.samples ?? 1,
    transport,
  });
  if (answers === null) return { outcome: fallback, via: "fallback", reason: "no JEV_API_KEY" };

  const [answer] = answers;
  const { verdict, confidence } = answer;
  if (verdict === REVIEW) {
    return { outcome: fallback, via: "fallback", reason: "split sample or inside the band", confidence };
  }
  if (confidence < (question.minConfidence ?? 0)) {
    return { outcome: fallback, via: "fallback", reason: "below minConfidence", verdict, confidence };
  }
  return { outcome: question.then?.[verdict] ?? verdict, via: "jev", verdict, confidence };
}

async function main(argv) {
  const lines = argv.includes("--lines");
  const [id, source = "-"] = argv.filter((arg) => arg !== "--lines");
  if (!id) {
    console.error("usage: node decide.mjs <question-id> [state-file | -] [--lines]   (- reads stdin)");
    return 2;
  }
  const state = readFileSync(source === "-" ? 0 : source, "utf8").trim();
  if (!state) {
    console.error("decide: the state is empty");
    return 2;
  }
  const question = findQuestion(id);
  if (!lines) {
    console.log(JSON.stringify(await decide(state, question)));
    return 0;
  }
  // ponytail: one call per line, in parallel; core retries a 429, batch the calls if rows reach the hundreds
  const states = state.split("\n").map((line) => line.trim()).filter(Boolean);
  const results = await Promise.all(states.map((line) => decide(line, question)));
  for (const [index, result] of results.entries()) {
    console.log(JSON.stringify({ state: states[index], ...result }));
  }
  return 0;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exit(await main(process.argv.slice(2)));
}
