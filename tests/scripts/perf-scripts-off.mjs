#!/usr/bin/env node
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import pixelmatch from "pixelmatch";
import { PNG } from "pngjs";
import { comparePixels, scriptsOffPairs, scriptsOffPasses } from "./perf-diff-lib.mjs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const [ref] = process.argv.slice(2);
if (!ref) {
  process.stderr.write("Usage: perf-scripts-off.mjs <ref>\n");
  process.exit(2);
}

const capturesRoot = process.env.KCC_PERF_CAPTURES ?? path.join(repoRoot, "tests", "results", "perf", "captures");
const dir = path.join(capturesRoot, ref);
if (!existsSync(dir)) {
  process.stderr.write(`No captures at ${dir}; run yarn perf:capture ${ref} first.\n`);
  process.exit(1);
}

const names = readdirSync(dir).filter((f) => f.endsWith(".png")).map((f) => f.slice(0, -4));
const { pairs, unpaired } = scriptsOffPairs(names);
for (const name of unpaired) process.stderr.write(`no partner for ${name}\n`);
if (pairs.length === 0) {
  process.stderr.write(`No scripts-off pairs in ${dir}.\n`);
  process.exit(1);
}

const heatDir = path.join(dir, "heat", "scripts-off");
mkdirSync(heatDir, { recursive: true });
const failed = [];
for (const pair of pairs) {
  const on = PNG.sync.read(readFileSync(path.join(dir, `${pair.on}.png`)));
  const off = PNG.sync.read(readFileSync(path.join(dir, `${pair.off}.png`)));
  const result = comparePixels(on, off);
  if (scriptsOffPasses(result)) {
    process.stdout.write(`pass ${pair.name}\n`);
    continue;
  }

  failed.push(pair.name);
  if (!result.sameSize) {
    process.stdout.write(`FAIL ${pair.name}: ${on.width}x${on.height} on, ${off.width}x${off.height} off\n`);
    continue;
  }

  const heat = new PNG({ width: on.width, height: on.height });
  pixelmatch(on.data, off.data, heat.data, on.width, on.height, { threshold: 0, includeAA: true });
  writeFileSync(path.join(heatDir, `${pair.name}.png`), PNG.sync.write(heat));
  process.stdout.write(`FAIL ${pair.name}: ${result.differing} pixels differ, largest channel delta ${result.maxDelta}\n`);
}

process.stdout.write(`${pairs.length - failed.length} of ${pairs.length} pairs identical; heat maps in ${heatDir}\n`);
process.exit(failed.length > 0 || unpaired.length > 0 ? 1 : 0);
