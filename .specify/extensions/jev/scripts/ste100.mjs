import { readFileSync } from "node:fs";
import { pathToFileURL } from "node:url";
import { ask, findQuestion, PASS } from "./core.mjs";

// Reports the antithesis shape in a planning artifact. It never blocks, because
// ste100-antithesis returns a review on nine cases of twenty.

const QUESTION = "ste100-antithesis";
const BATCH = 40;

// Prose only. A table row, a heading, a fenced block and an indented block are
// not sentences, and a question asked about them measures nothing.
const LIST_ITEM = /^\s*([-*+]\s|\d+\.\s)/;

export function sentences(markdown) {
  // Blocks first, sentences second. A list item is its own block, because
  // joining five bullets into one blob asks a question about nothing.
  const blocks = [];
  let current = [];
  let fenced = false;
  const flush = () => {
    if (current.length > 0) blocks.push(current.join(" "));
    current = [];
  };

  for (const line of markdown.split("\n")) {
    if (/^\s*```/.test(line)) {
      fenced = !fenced;
      flush();
      continue;
    }
    if (fenced) continue;
    if (/^\s*(\||#|>|-{3,}\s*$|\s{4,}\S)/.test(line)) {
      flush();
      continue;
    }
    if (line.trim().length === 0) {
      flush();
      continue;
    }
    if (LIST_ITEM.test(line)) {
      flush();
      current.push(line.replace(LIST_ITEM, ""));
      continue;
    }
    current.push(line);
  }
  flush();

  return blocks
    .flatMap((block) =>
      block
        .replace(/`[^`]*`/g, "CODE")
        .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
        .split(/(?<=[.!?])\s+(?=[A-Z`*_])/),
    )
    .map((sentence) => sentence.trim())
    .filter((sentence) => sentence.split(/\s+/).length >= 5);
}

async function main(files) {
  if (files.length === 0) {
    console.error("usage: node <jev>/scripts/ste100.mjs <file.md> [...]");
    return 2;
  }

  const question = findQuestion(QUESTION);

  let checked = 0;
  let flagged = 0;

  for (const file of files) {
    const found = sentences(readFileSync(file, "utf8"));
    for (let start = 0; start < found.length; start += BATCH) {
      const chunk = found.slice(start, start + BATCH);
      const questions = chunk.map((sentence, index) => ({
        ...question,
        id: `${QUESTION}__${start + index}`,
        text: `${question.text}\n\nSentence:\n${sentence}`,
      }));
      const answers = await ask(chunk.join("\n"), questions);
      if (answers === null) return 0;
      checked += chunk.length;
      for (const [index, answer] of answers.entries()) {
        if (answer.verdict === PASS) continue;
        flagged += 1;
        console.log(`\n${answer.verdict.toUpperCase()}  ${file}`);
        console.log(`  ${chunk[index]}`);
      }
    }
  }

  console.log(`\nste100: ${flagged} of ${checked} sentences need a look`);
  return 0;
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exit(await main(process.argv.slice(2)));
}
