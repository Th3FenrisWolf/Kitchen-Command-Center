import { describe, it, expect } from 'vitest'
import {
  budgetText,
  deltaText,
  failureLine,
  changeText,
  formatNumber,
  parseReportArgs,
  renderComparisonHtml,
  renderComparisonMarkdown,
  renderHtml,
  renderMarkdown,
  resultsPathFor,
  summarize,
} from '../../../scripts/perf-report.mjs'

const identity = 'search-3 · phone · chromium · dark · bloomed'

const results = {
  captured: '2026-10-26T10:00:00Z',
  commit: 'abc1234',
  environment: { gpu: 'swiftshader' },
  cases: {
    [identity]: {
      frame_p95_ms: {
        value: 22.4,
        kind: 'timing',
        unit: 'ms',
        baseline: 15.1,
        delta: 7.3,
        budget: { metric: 'frame_p95_ms', mode: 'baseline', margin: { ratio: 1.25, floor: 4 } },
        status: 'fail',
      },
      frames_within_16_7_pct: {
        value: 66,
        kind: 'timing',
        unit: 'pct',
        budget: { metric: 'frames_within_16_7_pct', mode: 'target', limit: 95 },
        status: 'fail',
      },
      layers: { value: 70, kind: 'deterministic', unit: 'count', baseline: 70, delta: 0, status: 'pass' },
      filtered_elements: { value: 84, kind: 'deterministic', unit: 'count', status: 'report-only' },
    },
  },
  gaps: { [identity.replace('chromium', 'webkit')]: ['layers: WebKit exposes no layer tree'] },
}

describe('perf report', () => {
  it('reads perf-live results only in live mode', () => {
    expect(resultsPathFor('live', '/r')).toBe('/r/tests/results/perf-live/results.json')
    expect(resultsPathFor('local', '/r')).toBe('/r/tests/results/perf/results.json')
    expect(resultsPathFor(undefined, '/r')).toBe('/r/tests/results/perf/results.json')
  })

  it('words a ratio failure the way the contract does', () => {
    expect(failureLine(identity, 'frame_p95_ms', results.cases[identity].frame_p95_ms)).toBe(
      `${identity}: frame_p95_ms 22.4 > baseline 15.1 (x1.25, +4 ms)`,
    )
  })

  it('reads frames within budget as higher is better', () => {
    const record = results.cases[identity].frames_within_16_7_pct
    expect(failureLine(identity, 'frames_within_16_7_pct', record)).toBe(
      `${identity}: frames_within_16_7_pct 66 < target 95`,
    )
    expect(budgetText('frames_within_16_7_pct', record)).toBe('≥ 95')
  })

  it('shows the delta with its sign and percentage, and a dash with no baseline', () => {
    expect(deltaText({ delta: 7.3, baseline: 15.1 })).toBe('+7.3 (+48.34%)')
    expect(deltaText({ delta: -2, baseline: 10 })).toBe('-2 (-20%)')
    expect(deltaText({})).toBe('—')
  })

  it('counts statuses and collects failures', () => {
    const summary = summarize(results)
    expect(summary.counts).toMatchObject({ pass: 1, fail: 2, 'report-only': 1 })
    expect(summary.failures).toHaveLength(2)
  })

  it('writes each metric with value, budget, baseline, delta and status, and what was not captured', () => {
    const markdown = renderMarkdown(results)
    expect(markdown).toContain('| frame_p95_ms | 22.4 ms | x1.25, +4 ms | 15.1 ms | +7.3 (+48.34%) | FAIL |')
    expect(markdown).toContain('## Not captured')
    expect(markdown).toContain('layers: WebKit exposes no layer tree (1 case)')
    const html = renderHtml(results)
    expect(html).toContain('<summary>search-3 · phone · chromium · dark · bloomed (fails)</summary>')
    expect(html).toContain('layers: WebKit exposes no layer tree')
  })

  it('keeps three significant digits below one, so a layout shift score is not rounded away', () => {
    expect(formatNumber(0.0042)).toBe('0.0042')
    expect(formatNumber(0.12345)).toBe('0.123')
    expect(formatNumber(22.456)).toBe('22.46')
    expect(formatNumber(0)).toBe('0')
  })

  it('labels idle GPU time as a share of one core, unnormalised', () => {
    const markdown = renderMarkdown({
      cases: { [identity]: { idle_gpu_pct: { value: 134.2, kind: 'timing', unit: 'pct-core', status: 'report-only' } } },
    })
    expect(markdown).toContain('| idle_gpu_pct | 134.2% of one core |')
  })

  it('names a report-only budget and a metric skipped on another platform', () => {
    const skipped = {
      value: 70,
      unit: 'count',
      status: 'skipped',
      budget: { metric: 'layers', mode: 'baseline', platformSensitive: true },
    }
    const reportOnly = {
      value: 0.2,
      unit: 'score',
      status: 'report-only',
      budget: { metric: 'cls_scroll', mode: 'report-only' },
    }
    expect(budgetText('cls_scroll', reportOnly)).toBe('report only')
    const summary = summarize({ cases: { [identity]: { layers: skipped, cls_scroll: reportOnly } } })
    expect(summary.counts).toMatchObject({ skipped: 1, 'report-only': 1 })
    expect(renderMarkdown({ cases: { [identity]: { layers: skipped } } })).toContain('| skipped: platform |')
  })

  it('escapes what it puts in the HTML', () => {
    const html = renderHtml({ cases: { '<b>': { x: { value: 1, unit: 'count', status: 'pass' } } } })
    expect(html).toContain('&lt;b&gt;')
    expect(html).not.toContain('<summary><b>')
  })
})

describe('perf report, before and after', () => {
  const metric = (value: number, extra = {}) => ({ value, kind: 'timing', unit: 'ms', status: 'pass', ...extra })
  const before = {
    captured: '2026-10-20T10:00:00Z',
    commit: 'aaa1111',
    cases: {
      [identity]: { frame_p95_ms: metric(24.7), lcp_ms: metric(1420), gone_ms: metric(5) },
      'home · phone · chromium · dark · bloomed': { lcp_ms: metric(900) },
    },
  }
  const after = {
    captured: '2026-10-27T10:00:00Z',
    commit: 'bbb2222',
    cases: {
      [identity]: {
        frame_p95_ms: metric(9.2, { budget: { metric: 'frame_p95_ms', mode: 'target', limit: 16.7 } }),
        lcp_ms: metric(1800, { status: 'fail', budget: { metric: 'lcp_ms', mode: 'target', limit: 1500 } }),
        new_ms: metric(3),
      },
      'recipe · phone · chromium · dark · bloomed': { lcp_ms: metric(1000) },
    },
  }

  it('words a change with its sign and percentage, and a dash when either side is missing', () => {
    expect(changeText(24.7, 9.2)).toBe('-15.5 (-62.75%)')
    expect(changeText(1420, 1800)).toBe('+380 (+26.76%)')
    expect(changeText(10, 10)).toBe('0')
    expect(changeText(undefined, 10)).toBe('—')
    expect(changeText(10, undefined)).toBe('—')
    expect(changeText(0, 5)).toBe('+5')
  })

  it('reads the before run from --before and leaves the rest alone', () => {
    expect(parseReportArgs(['--before', 'b.json'])).toEqual({ before: 'b.json', after: undefined })
    expect(parseReportArgs(['--before=b.json', '--after', 'a.json'])).toEqual({ before: 'b.json', after: 'a.json' })
    expect(parseReportArgs([])).toEqual({ before: undefined, after: undefined })
  })

  it('puts before, after and change on one row, with the after budget and status', () => {
    const markdown = renderComparisonMarkdown(before, after)
    expect(markdown).toContain('Before: captured 2026-10-20T10:00:00Z at commit aaa1111.')
    expect(markdown).toContain('After: captured 2026-10-27T10:00:00Z at commit bbb2222.')
    expect(markdown).toContain('| Metric | Before | After | Change | Budget | Status |')
    expect(markdown).toContain('| frame_p95_ms | 24.7 ms | 9.2 ms | -15.5 (-62.75%) | ≤ 16.7 | pass |')
    expect(markdown).toContain('| lcp_ms | 1420 ms | 1800 ms | +380 (+26.76%) | ≤ 1500 | FAIL |')
  })

  it('shows a metric on one side only with a dash, and names cases on one side only', () => {
    const markdown = renderComparisonMarkdown(before, after)
    expect(markdown).toContain('| gone_ms | 5 ms | — | — | — | — |')
    expect(markdown).toContain('| new_ms | — | 3 ms | — | — | pass |')
    expect(markdown).toContain('## Only in before')
    expect(markdown).toContain('- home · phone · chromium · dark · bloomed')
    expect(markdown).toContain('## Only in after')
    expect(markdown).toContain('- recipe · phone · chromium · dark · bloomed')
  })

  it('lists the after failures and escapes the html', () => {
    expect(renderComparisonMarkdown(before, after)).toContain(`- ${identity}: lcp_ms 1800 > target 1500`)
    const html = renderComparisonHtml(before, after)
    expect(html).toContain('<th>Before</th><th>After</th><th>Change</th>')
    expect(html).toContain('<summary>search-3 · phone · chromium · dark · bloomed (fails)</summary>')
    const hostile = { cases: { '<b>': { x: metric(1) } } }
    expect(renderComparisonHtml(hostile, hostile)).not.toContain('<summary><b>')
  })
})
