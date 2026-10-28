#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { copyFileSync, existsSync, mkdtempSync, rmSync } from "node:fs";
import os from "node:os";
import { fileURLToPath } from "node:url";
import path from "node:path";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const e2eProject = path.join(repoRoot, "tests", "KCC.E2ETests", "KCC.E2ETests.csproj");

const FILTERS = ["page", "profile", "engine", "ramp", "visit"];

const COMMANDS = {
  perf: { testClass: "PerfMatrixTests", mode: "local", report: "perf-report.mjs" },
  "perf:baseline": { testClass: "PerfMatrixTests", mode: "baseline", report: "perf-report.mjs" },
  "perf:live": { testClass: "PerfMatrixTests", mode: "live", report: "perf-report.mjs", budgetsNeverFail: true },
  "perf:capture": { testClass: "LookCaptureTests", mode: "capture", positional: ["KCC_PERF_REF"] },
  "perf:diff": { report: "perf-diff.mjs", positional: [] },
  "perf:report": { report: "perf-report.mjs", positional: [] },
  "perf:scripts-off": { report: "perf-scripts-off.mjs", positional: [] },
};

const [name, ...args] = process.argv.slice(2);
const command = COMMANDS[name];
if (!command) {
  process.stderr.write(`Unknown perf command "${name}". Known: ${Object.keys(COMMANDS).join(", ")}\n`);
  process.exit(2);
}

const env = { ...process.env, KCC_PERF_MODE: command.mode };
const positional = [];
const reportFlags = {};
for (let i = 0; i < args.length; i++) {
  const arg = args[i];
  const filter = FILTERS.find((f) => arg === `--${f}` || arg.startsWith(`--${f}=`));
  const reportFlag = ["before", "after"].find((f) => arg === `--${f}` || arg.startsWith(`--${f}=`));
  if (reportFlag) {
    reportFlags[reportFlag] = path.resolve(arg.includes("=") ? arg.slice(arg.indexOf("=") + 1) : args[++i]);
  } else if (arg === "--check") {
    if (!command.budgetsNeverFail) env.KCC_PERF_CHECK = "1";
  }
  else if (filter) {
    env[`KCC_PERF_FILTER_${filter.toUpperCase()}`] = arg.includes("=") ? arg.slice(arg.indexOf("=") + 1) : args[++i];
  } else positional.push(arg);
}
(command.positional ?? []).forEach((variable, i) => { if (positional[i]) env[variable] = positional[i]; });

function run(cmd, cmdArgs) {
  return spawnSync(cmd, cmdArgs, { cwd: repoRoot, env, stdio: "inherit" }).status ?? 1;
}

const resultsPath = path.join(repoRoot, "tests", "results", command.mode === "live" ? "perf-live" : "perf", "results.json");

if (command.testClass && reportFlags.before) {
  const keep = path.join(mkdtempSync(path.join(os.tmpdir(), "kcc-perf-before-")), "results.json");
  copyFileSync(reportFlags.before, keep);
  reportFlags.before = keep;
}

let testStatus = 0;
if (command.testClass) {
  if (command.report) rmSync(resultsPath, { force: true });
  testStatus = run("dotnet", [
    "run", "--project", e2eProject, "-c", "Release", "--disable-build-servers",
    "--", "--treenode-filter", `/*/*/${command.testClass}/*`,
  ]);
}

if (command.testClass && command.report && !existsSync(resultsPath)) {
  process.stderr.write(`The ${command.testClass} run wrote no ${resultsPath}; no report rendered.\n`);
  process.exit(testStatus || 1);
}

let reportStatus = 0;
if (command.report) {
  const reportArgs = Object.entries(reportFlags).map(([flag, file]) => `--${flag}=${file}`);
  if (!command.testClass) reportArgs.push(...positional);
  reportStatus = run("node", [path.join(repoRoot, "tests", "scripts", command.report), ...reportArgs]);
}

if (!command.testClass) process.exit(reportStatus);
if (env.KCC_PERF_CHECK) process.exit(testStatus || reportStatus);
process.exit(testStatus);
