import { emptySummary } from "./util.mjs";
import { normalizeTunit, normalizeVitest } from "./normalize.mjs";
import { renderHtml } from "./template.mjs";
import { renderMarkdown } from "./markdown.mjs";

export function aggregateSummary(suites) {
  const total = emptySummary();
  for (const suite of suites) {
    for (const key of Object.keys(total)) total[key] += suite.summary?.[key] ?? 0;
  }
  return total;
}

function placeholderSuite(descriptor, status, errorDetail) {
  return { id: descriptor.id, label: descriptor.label, type: descriptor.type, status, errorDetail, summary: emptySummary(), durationMs: 0, groups: [] };
}

// sources: Array<{ descriptor:{id,label,type}, kind:'tunit-json'|'vitest-json'|'missing', content?, error? }>
export function buildReport(sources, opts = {}) {
  const repoRoot = opts.repoRoot ?? process.cwd();
  const suites = sources.map((src) => {
    if (src.kind === "missing") return placeholderSuite(src.descriptor, "missing", src.error ?? "No results found");
    try {
      if (src.kind === "tunit-json") {
        return normalizeTunit(JSON.parse(src.content), src.descriptor);
      }
      if (src.kind === "vitest-json") {
        return normalizeVitest(JSON.parse(src.content), src.descriptor, repoRoot);
      }
      throw new Error(`Unknown source kind: ${src.kind}`);
    } catch (e) {
      return placeholderSuite(src.descriptor, "error", String(e?.message ?? e));
    }
  });
  const report = { generatedAt: opts.generatedAt ?? new Date().toISOString(), summary: aggregateSummary(suites), suites };
  return { report, html: renderHtml(report), markdown: renderMarkdown(report) };
}
