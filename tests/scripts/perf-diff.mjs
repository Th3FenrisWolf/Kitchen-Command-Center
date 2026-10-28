#!/usr/bin/env node
import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import pixelmatch from "pixelmatch";
import { PNG } from "pngjs";
import { assetPath, diffShare, groupByPage, nextRound, pairCases, renderRoundPage, slug } from "./perf-diff-lib.mjs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const mockups = path.join(repoRoot, ".mockups", "006-performance-sweep");
const HEAT_DIR = "heat";

const [beforeRef, afterRef] = process.argv.slice(2);
if (!beforeRef || !afterRef) {
  process.stderr.write("Usage: perf-diff.mjs <before> <after>\n");
  process.exit(2);
}

const capturesRoot = process.env.KCC_PERF_CAPTURES ?? path.join(repoRoot, "tests", "results", "perf", "captures");
const beforeDir = path.join(capturesRoot, beforeRef);
const afterDir = path.join(capturesRoot, afterRef);
for (const dir of [beforeDir, afterDir]) {
  if (!existsSync(dir)) {
    process.stderr.write(`No captures at ${dir}; run yarn perf:capture first.\n`);
    process.exit(1);
  }
}

function listCases(dir) {
  const names = [];
  const walk = (current) => {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      if (entry.isDirectory()) {
        if (entry.name !== HEAT_DIR) walk(full);
      } else if (entry.name.endsWith(".png")) {
        names.push(path.relative(dir, full).slice(0, -4).split(path.sep).join("/"));
      }
    }
  };
  walk(dir);
  return names;
}

function diffCase(name) {
  const before = PNG.sync.read(readFileSync(path.join(beforeDir, `${name}.png`)));
  const after = PNG.sync.read(readFileSync(path.join(afterDir, `${name}.png`)));
  const width = Math.max(before.width, after.width);
  const height = Math.max(before.height, after.height);
  const pad = (png) => {
    if (png.width === width && png.height === height) return png.data;
    const out = new PNG({ width, height });
    PNG.bitblt(png, out, 0, 0, png.width, png.height, 0, 0);
    return out.data;
  };
  const heat = new PNG({ width, height });
  const differing = pixelmatch(pad(before), pad(after), heat.data, width, height, { threshold: 0.1 });
  const heatPath = path.join(afterDir, HEAT_DIR, `${beforeRef}-vs-${afterRef}`, `${name}.png`);
  mkdirSync(path.dirname(heatPath), { recursive: true });
  writeFileSync(heatPath, PNG.sync.write(heat));
  return { heatPath, share: diffShare(differing, width * height) };
}

const { paired, onlyBefore, onlyAfter } = pairCases(listCases(beforeDir), listCases(afterDir));
for (const name of onlyBefore) process.stderr.write(`only in ${beforeRef}: ${name}\n`);
for (const name of onlyAfter) process.stderr.write(`only in ${afterRef}: ${name}\n`);
if (paired.length === 0) {
  process.stderr.write("No case appears in both capture sets.\n");
  process.exit(1);
}

mkdirSync(mockups, { recursive: true });
const round = nextRound(readdirSync(mockups));
const template = readFileSync(path.join(mockups, "_round.html"), "utf8");

function publish(src, name, side) {
  const rel = assetPath(round, side, name);
  const dest = path.join(mockups, rel);
  mkdirSync(path.dirname(dest), { recursive: true });
  copyFileSync(src, dest);
  return rel;
}

const results = new Map(paired.map((name) => [name, diffCase(name)]));
for (const [page, names] of groupByPage(paired)) {
  const cases = names.map((name) => ({
    name,
    share: results.get(name).share,
    before: publish(path.join(beforeDir, `${name}.png`), name, "before"),
    after: publish(path.join(afterDir, `${name}.png`), name, "after"),
    heat: publish(results.get(name).heatPath, name, "heat"),
  }));
  const file = `look-${round}-${slug(page)}.html`;
  writeFileSync(path.join(mockups, file), renderRoundPage(template, { round, page, cases }));
  process.stdout.write(`${file}: ${cases.length} cases\n`);
}
process.stdout.write(`Serve at http://localhost:4310/006-performance-sweep/ (round ${round}).\n`);
