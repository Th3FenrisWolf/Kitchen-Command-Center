#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from "node:fs";
import path from "node:path";
import { DOTNET_RESULTS_DIR, SUITES } from "./suites.mjs";
import { buildReport } from "./combine.mjs";
import { pickNewestByMtime, computeExitCode, isTunitReportFor } from "./util.mjs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const resultsDir = path.join(repoRoot, "tests", "results");
const outFile = path.join(resultsDir, "combined-report.html");
const summaryFile = path.join(resultsDir, "combined-report.md");
const dotnetResultsDir = path.join(repoRoot, DOTNET_RESULTS_DIR);

function log(msg) { process.stdout.write(`\n> ${msg}\n`); }

function findTunitReports(projectDir) {
  let names;
  try { names = readdirSync(dotnetResultsDir); } catch { return []; }
  return names
    .filter((name) => isTunitReportFor(name, projectDir))
    .map((name) => path.join(dotnetResultsDir, name))
    .map((full) => ({ path: full, mtimeMs: statSync(full).mtimeMs }));
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
// A suite that writes no report this run must show as missing, not pass on an earlier run's report.
rmSync(dotnetResultsDir, { recursive: true, force: true });

log(`Running .NET test suites (dotnet test -- --report-html --results-directory ${DOTNET_RESULTS_DIR})`);
spawnSync("dotnet", ["test", "--", "--report-html", "--results-directory", dotnetResultsDir], { cwd: repoRoot, stdio: "inherit", shell: false });

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

const sources = SUITES.map((suite) => {
  const descriptor = { id: suite.id, label: suite.label, type: suite.type };
  if (suite.type === "dotnet") {
    const newest = pickNewestByMtime(findTunitReports(suite.projectDir));
    if (!newest) return { descriptor, kind: "missing", error: `No *.tunit-report.json for ${suite.projectDir} under ${DOTNET_RESULTS_DIR}` };
    return { descriptor, kind: "tunit-json", content: readFileSync(newest, "utf8") };
  }
  const outAbs = path.join(repoRoot, suite.outputFile);
  if (!existsSync(outAbs)) return { descriptor, kind: "missing", error: `vitest did not produce ${suite.outputFile}` };
  return { descriptor, kind: "vitest-json", content: readFileSync(outAbs, "utf8") };
});

const { report, html, markdown } = buildReport(sources, { repoRoot });
writeFileSync(outFile, html, "utf8");
writeFileSync(summaryFile, markdown, "utf8");
log(`Combined report: ${outFile}`);
process.stdout.write(`  ${report.summary.passed} passed · ${report.summary.failed} failed · ${report.summary.skipped} skipped across ${report.suites.length} suites\n`);
for (const s of report.suites) if (s.status !== "ok") process.stdout.write(`  ! ${s.label}: ${s.errorDetail}\n`);
openInBrowser(outFile);
process.exit(computeExitCode(report));
