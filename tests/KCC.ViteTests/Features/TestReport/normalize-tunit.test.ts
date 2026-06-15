import { describe, it, expect } from "vitest";
import { normalizeTunit } from "../../../../scripts/test-report/normalize.mjs";

const descriptor = { id: "unit", label: "Unit" };

const data = {
  machineName: "BOX",
  operatingSystem: "Windows",
  runtimeVersion: ".NET 10.0.1",
  tunitVersion: "1.27.0.0",
  totalDurationMs: 592,
  summary: { total: 3, passed: 1, failed: 2, skipped: 0, cancelled: 0, timedOut: 0 },
  groups: [
    {
      className: "AccountViewModelTests",
      namespace: "KCC.UnitTests.Features.Pages.Account",
      summary: { total: 3, passed: 1, failed: 2, skipped: 0, cancelled: 0, timedOut: 0 },
      tests: [
        { displayName: "Passes", status: "passed", durationMs: 0.3, filePath: "A.cs", lineNumber: 10, retryAttempt: 0 },
        { displayName: "FailsStr", status: "failed", durationMs: 5, filePath: "A.cs", lineNumber: 20, errorMessage: "boom" },
        { displayName: "FailsObj", status: "failed", durationMs: 6, exception: { message: "kaboom", stackTrace: "  at X" } },
      ],
    },
  ],
};

describe("normalizeTunit", () => {
  it("maps assembly data to the unified suite shape", () => {
    const suite = normalizeTunit(data, descriptor);
    expect(suite.id).toBe("unit");
    expect(suite.type).toBe("dotnet");
    expect(suite.status).toBe("ok");
    expect(suite.durationMs).toBe(592);
    expect(suite.summary.failed).toBe(2);
    expect(suite.meta).toMatchObject({ framework: "TUnit", frameworkVersion: "1.27.0.0", machineName: "BOX" });
    expect(suite.groups[0].name).toBe("AccountViewModelTests");
  });

  it("extracts failure text from a string field", () => {
    const suite = normalizeTunit(data, descriptor);
    const t = suite.groups[0].tests.find((x) => x.name === "FailsStr");
    expect(t).toMatchObject({ status: "failed", errorMessage: "boom", lineNumber: 20 });
  });

  it("extracts failure text from an object-shaped exception field", () => {
    const suite = normalizeTunit(data, descriptor);
    const t = suite.groups[0].tests.find((x) => x.name === "FailsObj");
    expect(t.errorMessage).toContain("kaboom");
    expect(t.errorMessage).toContain("at X");
  });

  it("tolerates absent groups", () => {
    const suite = normalizeTunit({ summary: { total: 0 } }, descriptor);
    expect(suite.groups).toEqual([]);
  });
});
