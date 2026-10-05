import { readFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { ask, findQuestion, REVIEW } from "./core.mjs";
import { sentences } from "./ste100.mjs";

// Labels each prose sentence of an agent document. An instruction file keeps
// rules, facts and pointers, so every other label is printed for a reader.

const QUESTION = "doc-statement-kind";
const BATCH = 40;
const KEEP = new Set(["rule", "fact", "pointer"]);

async function main(argv) {
  const all = argv.includes("--all");
  const files = argv.filter((arg) => arg !== "--all");
  if (files.length === 0) {
    console.error("usage: node <jev>/scripts/doc-kinds.mjs [--all] <file.md> [...]");
    return 2;
  }

  const question = findQuestion(QUESTION);

  const totals = {};
  for (const file of files) {
    const found = sentences(readFileSync(file, "utf8"));
    const counts = (totals[file] = {});
    for (let start = 0; start < found.length; start += BATCH) {
      const chunk = found.slice(start, start + BATCH);
      const questions = chunk.map((sentence, index) => ({
        ...question,
        id: `${QUESTION}__${start + index}`,
        text: `${question.text}\n\nSentence:\n${sentence}`,
      }));
      const answers = await ask(`File: ${file}`, questions);
      if (answers === null) return 0;
      for (const [index, answer] of answers.entries()) {
        const kind =
          answer.verdict !== REVIEW && answer.confidence < (question.minConfidence ?? 0) ? REVIEW : answer.verdict;
        counts[kind] = (counts[kind] ?? 0) + 1;
        if (!all && KEEP.has(kind)) continue;
        console.log(`${kind.padEnd(9)} ${answer.confidence.toFixed(2)}  ${file}`);
        console.log(`  ${chunk[index]}`);
      }
    }
  }

  console.log();
  console.table(totals);
  return 0;
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exit(await main(process.argv.slice(2)));
}
