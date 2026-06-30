import { describe, it, expect } from "vitest";
import { SUITES } from "../../../scripts/suites.mjs";

describe("SUITES registry", () => {
  it("declares the five suites with required fields", () => {
    expect(SUITES.map((s) => s.id)).toEqual([
      "unit",
      "integration",
      "e2e",
      "web-frontend",
      "admin-frontend",
    ]);
    for (const s of SUITES) {
      expect(s.label).toBeTruthy();
      expect(["dotnet", "vitest"]).toContain(s.type);
    }
  });

  it("dotnet suites carry a projectDir; vitest suites carry cwd + outputFile", () => {
    for (const s of SUITES) {
      if (s.type === "dotnet") expect(s.projectDir).toBeTruthy();
      else {
        expect(s.cwd).toBeTruthy();
        expect(s.outputFile).toMatch(/^tests\/results\/vitest-.*\.json$/);
      }
    }
  });
});
