// Pure helpers shared across the report tool. No I/O here.

export function emptySummary() {
  return { total: 0, passed: 0, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 };
}

const HTML_ESCAPES = { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" };
export function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (c) => HTML_ESCAPES[c]);
}

export function fmtDuration(ms) {
  if (ms == null || Number.isNaN(ms)) return "—";
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

// entries: Array<{ path: string, mtimeMs: number }>
export function pickNewestByMtime(entries) {
  if (!entries || entries.length === 0) return null;
  return entries.reduce((best, e) => (e.mtimeMs > best.mtimeMs ? e : best)).path;
}

// report: { summary:{failed}, suites:[{status}] } -> 0 (clean) or 1 (problem)
export function computeExitCode(report) {
  if ((report.summary?.failed ?? 0) > 0) return 1;
  if ((report.suites ?? []).some((s) => s.status !== "ok")) return 1;
  return 0;
}
