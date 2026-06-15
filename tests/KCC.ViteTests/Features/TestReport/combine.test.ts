import { describe, it, expect } from "vitest";
import { gzipSync } from "node:zlib";
import { buildReport, aggregateSummary } from "../../../../scripts/test-report/combine.mjs";

function tunitHtml(data: unknown): string {
  return `<script id="test-data" data-compressed="gzip">${gzipSync(Buffer.from(JSON.stringify(data), "utf8")).toString("base64")}</script>`;
}

describe("aggregateSummary", () => {
  it("sums each summary field across suites", () => {
    const total = aggregateSummary([
      { summary: { total: 2, passed: 2, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 } },
      { summary: { total: 3, passed: 1, failed: 2, skipped: 0, cancelled: 0, timedOut: 0 } },
    ]);
    expect(total).toMatchObject({ total: 5, passed: 3, failed: 2 });
  });
});

describe("buildReport", () => {
  const sources = [
    {
      descriptor: { id: "unit", label: "Unit", type: "dotnet" },
      kind: "tunit-html",
      content: tunitHtml({ summary: { total: 1, passed: 1 }, groups: [{ className: "C", summary: { total: 1, passed: 1 }, tests: [{ displayName: "t", status: "passed", durationMs: 1 }] }] }),
    },
    {
      descriptor: { id: "web-frontend", label: "Web Frontend", type: "vitest" },
      kind: "vitest-json",
      content: JSON.stringify({ numTotalTests: 1, numPassedTests: 1, numFailedTests: 0, numPendingTests: 0, testResults: [{ name: "x/y.test.ts", startTime: 0, endTime: 2, assertionResults: [{ ancestorTitles: ["G"], title: "ok", status: "passed", duration: 2, failureMessages: [] }] }] }),
    },
    { descriptor: { id: "admin-frontend", label: "Admin Frontend", type: "vitest" }, kind: "missing", error: "No results found" },
  ];

  it("merges every source into one report + html", () => {
    const { report, html } = buildReport(sources, { repoRoot: "/repo", generatedAt: "T" });
    expect(report.suites.map((s) => s.status)).toEqual(["ok", "ok", "missing"]);
    expect(report.summary).toMatchObject({ total: 2, passed: 2, failed: 0 });
    expect(html).toContain("Combined Test Report");
    expect(html).toContain('data-tab="suite-unit"');
  });

  it("captures a bad source as an error suite instead of throwing", () => {
    const { report } = buildReport([{ descriptor: { id: "unit", label: "Unit", type: "dotnet" }, kind: "tunit-html", content: "<html>no data</html>" }], {});
    expect(report.suites[0].status).toBe("error");
    expect(report.suites[0].errorDetail).toBeTruthy();
  });
});
