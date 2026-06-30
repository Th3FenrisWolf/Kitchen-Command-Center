#!/usr/bin/env node
// One command: run every suite, merge results, write + open the combined report.
import { spawnSync } from "node:child_process";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from "node:fs";
import path from "node:path";
import { SUITES } from "./suites.mjs";
import { buildReport } from "./combine.mjs";
import { pickNewestByMtime, computeExitCode } from "./util.mjs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const resultsDir = path.join(repoRoot, "tests", "results");
const outFile = path.join(resultsDir, "combined-report.html");

function log(msg) { process.stdout.write(`\n> ${msg}\n`); }

// Recursively find */TestResults/*-report.html under a project's bin folder.
function findTunitReports(projectDir) {
  const binDir = path.join(repoRoot, projectDir, "bin");
  const out = [];
  const walk = (dir) => {
    let entries;
    try { entries = readdirSync(dir, { withFileTypes: true }); } catch { return; }
    for (const e of entries) {
      const full = path.join(dir, e.name);
      if (e.isDirectory()) walk(full);
      else if (e.isFile() && /-report\.html$/i.test(e.name) && /[\\/]TestResults[\\/]/.test(full)) {
        out.push({ path: full, mtimeMs: statSync(full).mtimeMs });
      }
    }
  };
  walk(binDir);
  return out;
}

// Resolve a suite's local vitest bin via its own node_modules (cross-platform, no shell).
function vitestBin(cwdAbs) {
  const require = createRequire(path.join(cwdAbs, "package.json"));
  const pkgJson = require.resolve("vitest/package.json");
  const bin = require(pkgJson).bin?.vitest ?? "vitest.mjs";
  return path.join(path.dirname(pkgJson), bin);
}

function openInBrowser(file) {
  try {
    if (process.platform === "win32") spawnSync("cmd", ["/c", "start", "", file], { stdio: "ignore" });
    else if (process.platform === "darwin") spawnSync("open", [file], { stdio: "ignore" });
    else spawnSync("xdg-open", [file], { stdio: "ignore" });
  } catch { /* opening is best-effort */ }
}

mkdirSync(resultsDir, { recursive: true });

// 1) Run the three TUnit suites in one solution-level invocation, with HTML report on.
log("Running .NET test suites (dotnet test -- --report-html)");
spawnSync("dotnet", ["test", "--", "--report-html"], { cwd: repoRoot, stdio: "inherit", shell: false });

// 2) Run each vitest suite, writing its json next to the combined report.
for (const suite of SUITES.filter((s) => s.type === "vitest")) {
  const cwdAbs = path.join(repoRoot, suite.cwd);
  const outAbs = path.join(repoRoot, suite.outputFile);
  log(`Running ${suite.label} (vitest)`);
  let bin;
  try {
    bin = vitestBin(cwdAbs);
  } catch {
    process.stdout.write(`  ! ${suite.label}: vitest not found in ${suite.cwd} — skipping\n`);
    continue;
  }
  spawnSync(process.execPath, [bin, "run", "--reporter=default", "--reporter=json", `--outputFile=${outAbs}`], { cwd: cwdAbs, stdio: "inherit" });
}

// 3) Collect every suite's output into combine sources.
const sources = SUITES.map((suite) => {
  const descriptor = { id: suite.id, label: suite.label, type: suite.type };
  if (suite.type === "dotnet") {
    const newest = pickNewestByMtime(findTunitReports(suite.projectDir));
    if (!newest) return { descriptor, kind: "missing", error: `No *-report.html under ${suite.projectDir}/bin` };
    return { descriptor, kind: "tunit-html", content: readFileSync(newest, "utf8") };
  }
  const outAbs = path.join(repoRoot, suite.outputFile);
  if (!existsSync(outAbs)) return { descriptor, kind: "missing", error: `vitest did not produce ${suite.outputFile}` };
  return { descriptor, kind: "vitest-json", content: readFileSync(outAbs, "utf8") };
});

// 4) Build, write, open, and exit with a meaningful code.
const { report, html } = buildReport(sources, { repoRoot });
writeFileSync(outFile, html, "utf8");
log(`Combined report: ${outFile}`);
process.stdout.write(`  ${report.summary.passed} passed · ${report.summary.failed} failed · ${report.summary.skipped} skipped across ${report.suites.length} suites\n`);
for (const s of report.suites) if (s.status !== "ok") process.stdout.write(`  ! ${s.label}: ${s.errorDetail}\n`);
openInBrowser(outFile);
process.exit(computeExitCode(report));
