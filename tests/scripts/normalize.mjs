import { emptySummary } from "./util.mjs";

// TUnit's failure-text field name/shape is unconfirmed (the committed test base
// in this worktree doesn't compile, so a live failure couldn't be probed). Pull it
// defensively from the likely fields, handling both a plain string and an object
// shape such as { message, stackTrace }.
function extractError(t) {
  const candidates = [t.errorMessage, t.exception, t.message, t.error, t.output, t.result];
  for (const c of candidates) {
    if (typeof c === "string" && c.length > 0) return c;
    if (c && typeof c === "object") {
      const msg = c.message ?? c.Message;
      const stack = c.stackTrace ?? c.StackTrace ?? c.stack;
      const text = [msg, stack].filter((s) => typeof s === "string" && s.length > 0).join("\n");
      if (text.length > 0) return text;
    }
  }
  return undefined;
}

// TUnit embedded data + descriptor -> unified Suite.
export function normalizeTunit(data, descriptor) {
  const groups = (data.groups || []).map((g) => ({
    name: g.className,
    namespace: g.namespace,
    summary: { ...emptySummary(), ...(g.summary || {}) },
    tests: (g.tests || []).map((t) => ({
      name: t.displayName ?? t.methodName ?? "(unnamed)",
      status: t.status,
      durationMs: t.durationMs ?? 0,
      filePath: t.filePath,
      lineNumber: t.lineNumber,
      errorMessage: extractError(t),
      retryAttempt: t.retryAttempt,
    })),
  }));
  return {
    id: descriptor.id,
    label: descriptor.label,
    type: "dotnet",
    status: "ok",
    summary: { ...emptySummary(), ...(data.summary || {}) },
    durationMs: data.totalDurationMs ?? 0,
    meta: {
      machineName: data.machineName,
      operatingSystem: data.operatingSystem,
      runtimeVersion: data.runtimeVersion,
      framework: "TUnit",
      frameworkVersion: data.tunitVersion,
    },
    groups,
  };
}

// vitest assertion status -> unified status.
export function mapVitestStatus(status) {
  if (status === "passed") return "passed";
  if (status === "failed") return "failed";
  return "skipped"; // pending | todo | skipped | disabled
}

// vitest --reporter=json output + descriptor -> unified Suite.
// repoRoot makes the absolute file paths repo-relative for display.
export function normalizeVitest(json, descriptor, repoRoot) {
  const rootFwd = String(repoRoot).replace(/\\/g, "/").replace(/\/$/, "");
  const toRel = (abs) => {
    if (!abs) return abs;
    const fwd = String(abs).replace(/\\/g, "/");
    return fwd.startsWith(rootFwd + "/") ? fwd.slice(rootFwd.length + 1) : fwd;
  };

  const groups = new Map();
  let minStart = Infinity;
  let maxEnd = -Infinity;

  for (const file of json.testResults || []) {
    const filePath = toRel(file.name);
    if (typeof file.startTime === "number") minStart = Math.min(minStart, file.startTime);
    if (typeof file.endTime === "number") maxEnd = Math.max(maxEnd, file.endTime);

    for (const a of file.assertionResults || []) {
      const groupName = (a.ancestorTitles && a.ancestorTitles[0]) || filePath || "(ungrouped)";
      if (!groups.has(groupName)) {
        groups.set(groupName, { name: groupName, summary: emptySummary(), tests: [], filePath });
      }
      const group = groups.get(groupName);
      const status = mapVitestStatus(a.status);
      group.tests.push({
        name: a.title,
        status,
        durationMs: a.duration ?? 0,
        filePath,
        errorMessage: a.failureMessages && a.failureMessages.length ? a.failureMessages.join("\n") : undefined,
      });
      group.summary.total += 1;
      group.summary[status] += 1;
    }
  }

  const summary = emptySummary();
  summary.total = json.numTotalTests ?? 0;
  summary.passed = json.numPassedTests ?? 0;
  summary.failed = json.numFailedTests ?? 0;
  summary.skipped = (json.numPendingTests ?? 0) + (json.numTodoTests ?? 0);

  const durationMs =
    minStart !== Infinity && maxEnd !== -Infinity ? maxEnd - minStart : 0;

  return {
    id: descriptor.id,
    label: descriptor.label,
    type: "vitest",
    status: "ok",
    summary,
    durationMs,
    meta: { framework: "vitest" },
    groups: [...groups.values()],
  };
}
