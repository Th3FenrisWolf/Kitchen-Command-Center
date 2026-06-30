// Registry of every test suite folded into the combined report.
// Paths are relative to the repo root; run.mjs resolves them.
export const SUITES = [
  { id: "unit", label: "Unit", type: "dotnet", projectDir: "tests/KCC.UnitTests" },
  { id: "integration", label: "Integration", type: "dotnet", projectDir: "tests/KCC.IntegrationTests" },
  { id: "e2e", label: "E2E", type: "dotnet", projectDir: "tests/KCC.E2ETests" },
  { id: "web-frontend", label: "Web Frontend", type: "vitest", cwd: "src/KCC.Web", outputFile: "tests/results/vitest-web-frontend.json" },
  { id: "admin-frontend", label: "Admin Frontend", type: "vitest", cwd: "src/KCC.Admin/Client", outputFile: "tests/results/vitest-admin-frontend.json" },
];
