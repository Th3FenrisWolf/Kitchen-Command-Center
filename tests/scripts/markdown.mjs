// GitHub strips <style> and <script> from job summaries, so the standalone HTML
// report cannot be embedded there — the same report data is re-rendered as the
// GitHub-flavored markdown subset the summary does render.
import { fmtDuration } from "./util.mjs";

// GitHub rejects a step summary larger than 1MB; stay well under it.
const MAX_BYTES = 900 * 1024;
const MAX_DETAILED_FAILURES = 40;
const MAX_ERROR_CHARS = 1200;

const suiteIcon = (suite) => (suite.status !== "ok" ? "⚠️" : suite.summary.failed > 0 ? "❌" : "✅");

function cell(value) {
  return String(value ?? "—").replace(/\|/g, "\\|").replace(/\s*\r?\n\s*/g, " ");
}

// A fence longer than any backtick run in the text keeps stack traces from breaking out.
function fenced(text) {
  const longestRun = Math.max(0, ...[...String(text).matchAll(/`+/g)].map((m) => m[0].length));
  const fence = "`".repeat(Math.max(3, longestRun + 1));
  return `${fence}\n${text}\n${fence}`;
}

function failuresOf(report) {
  const out = [];
  for (const suite of report.suites) {
    for (const group of suite.groups ?? []) {
      for (const test of group.tests ?? []) {
        if (test.status === "passed" || test.status === "skipped") continue;
        out.push({ suite, group, test });
      }
    }
  }
  return out;
}

function headline(report) {
  const s = report.summary;
  const broken = report.suites.filter((su) => su.status !== "ok");
  const icon = s.failed > 0 ? "❌" : broken.length > 0 ? "⚠️" : "✅";
  const parts = [`**${s.passed} passed**`, `${s.failed} failed`, `${s.skipped} skipped`];
  if (s.timedOut) parts.push(`${s.timedOut} timed out`);
  if (s.cancelled) parts.push(`${s.cancelled} cancelled`);
  parts.push(`${report.suites.length} suites`);
  return `${icon} ${parts.join(" · ")}`;
}

function overviewTable(report) {
  const rows = report.suites.map((su) => {
    const s = su.summary;
    const counts =
      su.status === "ok"
        ? [`${s.passed} / ${s.total}`, s.failed || "—", s.skipped || "—", fmtDuration(su.durationMs)]
        : [cell(su.status), "—", "—", "—"];
    return `| ${suiteIcon(su)} ${cell(su.label)} | ${cell(su.type)} | ${counts.join(" | ")} |`;
  });
  return [
    "| Suite | Type | Passed | Failed | Skipped | Duration |",
    "| --- | --- | --: | --: | --: | --: |",
    ...rows,
  ].join("\n");
}

function failureBlock(entry) {
  const { suite, group, test } = entry;
  const location = test.filePath ? `${test.filePath}${test.lineNumber ? `:${test.lineNumber}` : ""}` : "";
  const error = test.errorMessage
    ? String(test.errorMessage).slice(0, MAX_ERROR_CHARS) +
      (String(test.errorMessage).length > MAX_ERROR_CHARS ? "\n… truncated" : "")
    : "";
  const lines = [`**${suite.label} › ${group.name} › ${test.name}**`];
  if (location) lines.push("", `\`${location}\``);
  if (error) lines.push("", fenced(error));
  return lines.join("\n");
}

export function renderMarkdown(report) {
  const sections = ["## Combined Test Report", "", headline(report), "", overviewTable(report)];

  const broken = report.suites.filter((su) => su.status !== "ok");
  if (broken.length > 0) {
    sections.push("", "### ⚠️ Suites without results", "");
    for (const su of broken) sections.push(`- **${cell(su.label)}** — ${cell(su.errorDetail || su.status)}`);
  }

  const failures = failuresOf(report);
  if (failures.length > 0) {
    sections.push("", `### ❌ Failures (${failures.length})`, "");
    const shown = failures.slice(0, MAX_DETAILED_FAILURES);
    for (const entry of shown) sections.push(failureBlock(entry), "");
    if (failures.length > shown.length) {
      sections.push(`_${failures.length - shown.length} further failures omitted — see the full report artifact._`, "");
    }
  }

  sections.push("", `<sub>Generated ${cell(report.generatedAt)}</sub>`, "");

  const markdown = sections.join("\n");
  if (Buffer.byteLength(markdown, "utf8") <= MAX_BYTES) return markdown;

  const trimmed = [
    "## Combined Test Report",
    "",
    headline(report),
    "",
    overviewTable(report),
    "",
    "_Failure detail exceeded GitHub's job-summary size limit — see the full report artifact._",
    "",
  ].join("\n");
  return trimmed;
}
