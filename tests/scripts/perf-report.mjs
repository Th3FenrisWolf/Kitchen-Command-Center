import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { escapeHtml } from "./util.mjs";

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");

export function resultsPathFor(mode, root = repoRoot) {
  return path.join(root, "tests", "results", mode === "live" ? "perf-live" : "perf", "results.json");
}

export function formatNumber(value) {
  if (value == null || Number.isNaN(value)) return "—";
  const number = Number(value);
  const magnitude = number === 0 ? 0 : Math.floor(Math.log10(Math.abs(number)));
  return number.toLocaleString("en-US", { maximumFractionDigits: Math.max(2, 2 - magnitude), useGrouping: false });
}

function suffixFor(unit) {
  if (unit === "ms") return " ms";
  if (unit === "bytes") return " B";
  if (unit === "pct-core") return "% of one core";
  return "";
}

export function higherIsBetter(metric) {
  return metric.startsWith("frames_within_") || metric.includes("_frames_within_");
}

function marginLabel(margin, unit) {
  const suffix = suffixFor(unit);
  if (!margin) return "no increase";
  if (margin.ratio != null) return `x${formatNumber(margin.ratio)}, +${formatNumber(margin.floor ?? 0)}${suffix}`;
  if (margin.pct != null) return `+${formatNumber(margin.pct)}%, +${formatNumber(margin.floorBytes ?? 0)}${suffix} floor`;
  if (margin.add != null) return `+${formatNumber(margin.add)}`;
  return "no increase";
}

export function budgetText(metric, record) {
  const budget = record.budget;
  if (!budget) return "—";
  if (budget.mode === "report-only") return "report only";
  if (budget.mode === "target") {
    return `${higherIsBetter(metric) ? "≥" : "≤"} ${formatNumber(budget.limit)}`;
  }
  return marginLabel(budget.margin, record.unit);
}

export function failureLine(identity, metric, record) {
  const budget = record.budget;
  const worse = higherIsBetter(metric) ? "<" : ">";
  if (budget?.mode === "target") {
    return `${identity}: ${metric} ${formatNumber(record.value)} ${worse} target ${formatNumber(budget.limit)}`;
  }
  return `${identity}: ${metric} ${formatNumber(record.value)} ${worse} baseline ${formatNumber(record.baseline)} (${marginLabel(budget?.margin, record.unit)})`;
}

export function deltaText(record) {
  if (record.delta == null) return "—";
  const sign = record.delta > 0 ? "+" : "";
  const percent = record.baseline ? ` (${sign}${formatNumber((100 * record.delta) / record.baseline)}%)` : "";
  return `${sign}${formatNumber(record.delta)}${percent}`;
}

export function summarize(results) {
  const rows = [];
  for (const [identity, metrics] of Object.entries(results.cases ?? {})) {
    for (const [metric, record] of Object.entries(metrics)) {
      rows.push({
        identity,
        metric,
        value: record.value,
        unit: record.unit,
        kind: record.kind,
        budget: budgetText(metric, record),
        baseline: record.baseline,
        delta: deltaText(record),
        status: record.status,
        failure: record.status === "fail" ? failureLine(identity, metric, record) : null,
      });
    }
  }
  const failures = rows.filter((row) => row.failure).map((row) => row.failure);
  const counts = { pass: 0, fail: 0, "report-only": 0, skipped: 0 };
  for (const row of rows) counts[row.status] = (counts[row.status] ?? 0) + 1;
  return { rows, failures, counts, cases: Object.keys(results.cases ?? {}).length };
}

function gapEntries(results) {
  return Object.entries(results.gaps ?? {});
}

function gapsByReason(results) {
  const reasons = new Map();
  for (const [identity, gaps] of gapEntries(results)) {
    for (const gap of gaps) {
      if (!reasons.has(gap)) reasons.set(gap, []);
      reasons.get(gap).push(identity);
    }
  }
  return reasons;
}

const STATUS_MARK = { pass: "pass", fail: "FAIL", "report-only": "report only", skipped: "skipped: platform" };

function cell(value) {
  return String(value ?? "—").replace(/\|/g, "\\|").replace(/\s*\r?\n\s*/g, " ");
}

function runHeader(results) {
  const environment = Object.entries(results.environment ?? {}).map(([key, value]) => `${key}: ${value}`).join("; ");
  return { captured: results.captured ?? "—", commit: results.commit ?? "—", environment };
}

export function renderMarkdown(results) {
  const { rows, failures, counts, cases } = summarize(results);
  const header = runHeader(results);
  const lines = [
    "# Performance report",
    "",
    `Captured ${header.captured} at commit ${header.commit}.`,
    header.environment ? `Environment: ${header.environment}.` : "",
    "",
    `${cases} cases, ${counts.pass} pass, ${counts.fail} fail, ${counts["report-only"]} report only, ${counts.skipped} skipped.`,
    "",
  ];
  if (failures.length > 0) {
    lines.push("## Failures", "", ...failures.map((line) => `- ${line}`), "");
  }
  const byCase = new Map();
  for (const row of rows) {
    if (!byCase.has(row.identity)) byCase.set(row.identity, []);
    byCase.get(row.identity).push(row);
  }
  lines.push("## Cases", "");
  for (const [identity, caseRows] of byCase) {
    lines.push(`### ${identity}`, "", "| Metric | Value | Budget | Baseline | Delta | Status |", "| --- | ---: | --- | ---: | ---: | --- |");
    for (const row of caseRows) {
      const unit = suffixFor(row.unit);
      lines.push(`| ${cell(row.metric)} | ${formatNumber(row.value)}${unit} | ${cell(row.budget)} | ${row.baseline == null ? "—" : formatNumber(row.baseline) + unit} | ${cell(row.delta)} | ${STATUS_MARK[row.status] ?? row.status} |`);
    }
    lines.push("");
  }
  const reasons = gapsByReason(results);
  lines.push("## Not captured", "");
  if (reasons.size === 0) {
    lines.push("Every collector reported in every case.", "");
  } else {
    for (const [reason, identities] of reasons) {
      lines.push(`- ${reason} (${identities.length} ${identities.length === 1 ? "case" : "cases"})`);
    }
    lines.push("");
  }
  return lines.filter((line, index, all) => !(line === "" && all[index - 1] === "")).join("\n");
}

export function renderHtml(results) {
  const { rows, failures, counts, cases } = summarize(results);
  const header = runHeader(results);
  const byCase = new Map();
  for (const row of rows) {
    if (!byCase.has(row.identity)) byCase.set(row.identity, []);
    byCase.get(row.identity).push(row);
  }
  const sections = [...byCase].map(([identity, caseRows]) => {
    const body = caseRows.map((row) => {
      const unit = suffixFor(row.unit);
      return `<tr class="${escapeHtml(row.status)}"><td>${escapeHtml(row.metric)}</td><td class="n">${escapeHtml(formatNumber(row.value))}${unit}</td><td>${escapeHtml(row.budget)}</td><td class="n">${row.baseline == null ? "—" : escapeHtml(formatNumber(row.baseline)) + unit}</td><td class="n">${escapeHtml(row.delta)}</td><td>${escapeHtml(STATUS_MARK[row.status] ?? row.status)}</td></tr>`;
    }).join("");
    const failed = caseRows.some((row) => row.status === "fail");
    return `<details${failed ? " open" : ""}><summary>${escapeHtml(identity)}${failed ? " (fails)" : ""}</summary><table><thead><tr><th>Metric</th><th>Value</th><th>Budget</th><th>Baseline</th><th>Delta</th><th>Status</th></tr></thead><tbody>${body}</tbody></table></details>`;
  });
  const reasons = gapsByReason(results);
  const gaps = reasons.size === 0
    ? "<p>Every collector reported in every case.</p>"
    : `<ul>${[...reasons].map(([reason, identities]) => `<li>${escapeHtml(reason)} (${identities.length} ${identities.length === 1 ? "case" : "cases"})</li>`).join("")}</ul>`;
  const failureList = failures.length === 0 ? "" : `<h2>Failures</h2><ul>${failures.map((line) => `<li>${escapeHtml(line)}</li>`).join("")}</ul>`;
  return `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Performance report</title>
<style>
body{font:14px/1.4 system-ui,sans-serif;margin:2rem;color:#1c1b22}
table{border-collapse:collapse;margin:.5rem 0 1rem}
th,td{border:1px solid #c9c7d3;padding:.25rem .6rem;text-align:left}
td.n{text-align:right;font-variant-numeric:tabular-nums}
tr.fail{background:#fde2e2}
tr.report-only{color:#5d5b6a}
summary{cursor:pointer;font-weight:600;margin:.4rem 0}
</style></head><body>
<h1>Performance report</h1>
<p>Captured ${escapeHtml(header.captured)} at commit ${escapeHtml(header.commit)}.${header.environment ? ` Environment: ${escapeHtml(header.environment)}.` : ""}</p>
<p>${cases} cases, ${counts.pass} pass, ${counts.fail} fail, ${counts["report-only"]} report only, ${counts.skipped} skipped.</p>
${failureList}
<h2>Cases</h2>
${sections.join("\n")}
<h2>Not captured</h2>
${gaps}
</body></html>
`;
}

export function changeText(before, after) {
  if (before == null || after == null) return "—";
  const change = after - before;
  if (change === 0) return "0";
  const sign = change > 0 ? "+" : "";
  const percent = before ? ` (${sign}${formatNumber((100 * change) / before)}%)` : "";
  return `${sign}${formatNumber(change)}${percent}`;
}

export function parseReportArgs(args) {
  const parsed = { before: undefined, after: undefined };
  for (let i = 0; i < args.length; i++) {
    for (const key of ["before", "after"]) {
      if (args[i] === `--${key}`) parsed[key] = args[++i];
      else if (args[i].startsWith(`--${key}=`)) parsed[key] = args[i].slice(key.length + 3);
    }
  }
  return parsed;
}

function compareRows(before, after) {
  const beforeCases = before.cases ?? {};
  const afterSummary = summarize(after);
  const byCase = new Map();
  for (const identity of Object.keys(after.cases ?? {})) {
    if (!(identity in beforeCases)) continue;
    const afterMetrics = after.cases[identity];
    const rows = [];
    for (const metric of new Set([...Object.keys(beforeCases[identity]), ...Object.keys(afterMetrics)])) {
      const was = beforeCases[identity][metric];
      const now = afterMetrics[metric];
      rows.push({
        metric,
        unit: suffixFor((now ?? was).unit),
        before: was ? formatNumber(was.value) : null,
        after: now ? formatNumber(now.value) : null,
        change: changeText(was?.value, now?.value),
        budget: now ? budgetText(metric, now) : "—",
        status: now ? now.status : null,
      });
    }
    byCase.set(identity, rows);
  }
  const onlyBefore = Object.keys(beforeCases).filter((identity) => !(identity in (after.cases ?? {})));
  const onlyAfter = Object.keys(after.cases ?? {}).filter((identity) => !(identity in beforeCases));
  return { byCase, onlyBefore, onlyAfter, ...afterSummary };
}

function withUnit(value, unit) {
  return value == null ? "—" : value + unit;
}

function statusMark(status) {
  return status == null ? "—" : STATUS_MARK[status] ?? status;
}

export function renderComparisonMarkdown(before, after) {
  const { byCase, onlyBefore, onlyAfter, failures, counts } = compareRows(before, after);
  const was = runHeader(before);
  const now = runHeader(after);
  const lines = [
    "# Performance report, before and after",
    "",
    `Before: captured ${was.captured} at commit ${was.commit}.`,
    `After: captured ${now.captured} at commit ${now.commit}.`,
    now.environment ? `Environment: ${now.environment}.` : "",
    "",
    `${byCase.size} cases in both runs. After: ${counts.pass} pass, ${counts.fail} fail, ${counts["report-only"]} report only, ${counts.skipped} skipped.`,
    "",
  ];
  if (failures.length > 0) lines.push("## Failures", "", ...failures.map((line) => `- ${line}`), "");
  lines.push("## Cases", "");
  for (const [identity, rows] of byCase) {
    lines.push(`### ${identity}`, "", "| Metric | Before | After | Change | Budget | Status |", "| --- | ---: | ---: | ---: | --- | --- |");
    for (const row of rows) {
      lines.push(`| ${cell(row.metric)} | ${withUnit(row.before, row.unit)} | ${withUnit(row.after, row.unit)} | ${cell(row.change)} | ${cell(row.budget)} | ${statusMark(row.status)} |`);
    }
    lines.push("");
  }
  for (const [heading, identities] of [["Only in before", onlyBefore], ["Only in after", onlyAfter]]) {
    if (identities.length > 0) lines.push(`## ${heading}`, "", ...identities.map((identity) => `- ${identity}`), "");
  }
  return lines.filter((line, index, all) => !(line === "" && all[index - 1] === "")).join("\n");
}

export function renderComparisonHtml(before, after) {
  const { byCase, onlyBefore, onlyAfter, failures, counts } = compareRows(before, after);
  const was = runHeader(before);
  const now = runHeader(after);
  const sections = [...byCase].map(([identity, rows]) => {
    const body = rows.map((row) =>
      `<tr class="${escapeHtml(row.status ?? "")}"><td>${escapeHtml(row.metric)}</td><td class="n">${escapeHtml(withUnit(row.before, row.unit))}</td><td class="n">${escapeHtml(withUnit(row.after, row.unit))}</td><td class="n">${escapeHtml(row.change)}</td><td>${escapeHtml(row.budget)}</td><td>${escapeHtml(statusMark(row.status))}</td></tr>`).join("");
    const failed = rows.some((row) => row.status === "fail");
    return `<details${failed ? " open" : ""}><summary>${escapeHtml(identity)}${failed ? " (fails)" : ""}</summary><table><thead><tr><th>Metric</th><th>Before</th><th>After</th><th>Change</th><th>Budget</th><th>Status</th></tr></thead><tbody>${body}</tbody></table></details>`;
  });
  const list = (heading, identities) => identities.length === 0 ? "" : `<h2>${heading}</h2><ul>${identities.map((identity) => `<li>${escapeHtml(identity)}</li>`).join("")}</ul>`;
  const failureList = failures.length === 0 ? "" : `<h2>Failures</h2><ul>${failures.map((line) => `<li>${escapeHtml(line)}</li>`).join("")}</ul>`;
  return `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Performance report, before and after</title>
<style>
body{font:14px/1.4 system-ui,sans-serif;margin:2rem;color:#1c1b22}
table{border-collapse:collapse;margin:.5rem 0 1rem}
th,td{border:1px solid #c9c7d3;padding:.25rem .6rem;text-align:left}
td.n{text-align:right;font-variant-numeric:tabular-nums}
tr.fail{background:#fde2e2}
tr.report-only{color:#5d5b6a}
summary{cursor:pointer;font-weight:600;margin:.4rem 0}
</style></head><body>
<h1>Performance report, before and after</h1>
<p>Before: captured ${escapeHtml(was.captured)} at commit ${escapeHtml(was.commit)}.<br>After: captured ${escapeHtml(now.captured)} at commit ${escapeHtml(now.commit)}.${now.environment ? ` Environment: ${escapeHtml(now.environment)}.` : ""}</p>
<p>${byCase.size} cases in both runs. After: ${counts.pass} pass, ${counts.fail} fail, ${counts["report-only"]} report only, ${counts.skipped} skipped.</p>
${failureList}
<h2>Cases</h2>
${sections.join("\n")}
${list("Only in before", onlyBefore)}${list("Only in after", onlyAfter)}
</body></html>
`;
}

export function writeReports(resultsPath, beforePath) {
  const results = JSON.parse(fs.readFileSync(resultsPath, "utf8"));
  const directory = path.dirname(resultsPath);
  if (beforePath) {
    const before = JSON.parse(fs.readFileSync(beforePath, "utf8"));
    fs.writeFileSync(path.join(directory, "report.html"), renderComparisonHtml(before, results));
    fs.writeFileSync(path.join(directory, "report.md"), renderComparisonMarkdown(before, results));
    return summarize(results);
  }
  fs.writeFileSync(path.join(directory, "report.html"), renderHtml(results));
  fs.writeFileSync(path.join(directory, "report.md"), renderMarkdown(results));
  return summarize(results);
}

function main() {
  const { before, after } = parseReportArgs(process.argv.slice(2));
  const resultsPath = after ? path.resolve(after) : resultsPathFor(process.env.KCC_PERF_MODE);
  for (const [label, file] of [["results", resultsPath], ["before results", before && path.resolve(before)]]) {
    if (file && !fs.existsSync(file)) {
      console.error(`No ${label} at ${file}.${label === "results" ? " Run the perf command first." : ""}`);
      return 1;
    }
  }
  const { failures, counts, cases } = writeReports(resultsPath, before && path.resolve(before));
  console.log(`perf report${before ? " (before and after)" : ""}: ${cases} cases, ${counts.pass} pass, ${counts.fail} fail, ${counts["report-only"]} report only, ${counts.skipped} skipped`);
  for (const line of failures) console.log(`  FAIL ${line}`);
  console.log(`  ${path.join(path.dirname(resultsPath), "report.md")}`);
  return failures.length > 0 ? 1 : 0;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  process.exit(main());
}
