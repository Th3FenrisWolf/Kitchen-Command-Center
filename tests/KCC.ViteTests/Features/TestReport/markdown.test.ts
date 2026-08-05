import { describe, it, expect } from 'vitest'
import { renderMarkdown } from '../../../scripts/markdown.mjs'

const report = {
  generatedAt: '2026-06-13T15:05:30.000Z',
  summary: { total: 4, passed: 2, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
  suites: [
    {
      id: 'unit',
      label: 'Unit',
      type: 'dotnet',
      status: 'ok',
      summary: { total: 3, passed: 1, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
      durationMs: 592,
      meta: { framework: 'TUnit' },
      groups: [
        {
          name: 'AccountViewModelTests',
          namespace: 'KCC.UnitTests',
          summary: { total: 3, passed: 1, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
          tests: [
            { name: 'Passes', status: 'passed', durationMs: 1 },
            { name: 'Fails', status: 'failed', durationMs: 5, filePath: 'A.cs', lineNumber: 20, errorMessage: 'boom' },
            { name: 'Ignored', status: 'skipped', durationMs: 0 },
          ],
        },
      ],
    },
    {
      id: 'web-frontend',
      label: 'Web Frontend',
      type: 'vitest',
      status: 'ok',
      summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
      durationMs: 12,
      groups: [
        {
          name: 'RecipeCard',
          summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
          tests: [{ name: 'renders', status: 'passed', durationMs: 12 }],
        },
      ],
    },
    {
      id: 'admin-frontend',
      label: 'Admin Frontend',
      type: 'vitest',
      status: 'missing',
      errorDetail: 'No results found',
      summary: { total: 0, passed: 0, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
      durationMs: 0,
      groups: [],
    },
  ],
}

describe('renderMarkdown', () => {
  it('leads with a heading, totals and a per-suite table', () => {
    const md = renderMarkdown(report)
    expect(md.startsWith('## Combined Test Report')).toBe(true)
    expect(md).toContain('**2 passed** · 1 failed · 1 skipped · 3 suites')
    expect(md).toContain('| Suite | Type | Passed | Failed | Skipped | Duration |')
    expect(md).toContain('| ❌ Unit | dotnet | 1 / 3 | 1 | 1 | 592ms |')
    expect(md).toContain('| ✅ Web Frontend | vitest | 1 / 1 | — | — | 12ms |')
  })

  it('lists suites that produced no results', () => {
    const md = renderMarkdown(report)
    expect(md).toContain('### ⚠️ Suites without results')
    expect(md).toContain('**Admin Frontend** — No results found')
    expect(md).toContain('| ⚠️ Admin Frontend | vitest | missing |')
  })

  it('details each failure with its location and error, skipping passes', () => {
    const md = renderMarkdown(report)
    expect(md).toContain('### ❌ Failures (1)')
    expect(md).toContain('**Unit › AccountViewModelTests › Fails**')
    expect(md).toContain('`A.cs:20`')
    expect(md).toContain('```\nboom\n```')
    expect(md).not.toContain('Passes')
    expect(md).not.toContain('Ignored')
  })

  it('omits the failure section entirely when everything passed', () => {
    const md = renderMarkdown({
      generatedAt: 'T',
      summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
      suites: [
        {
          id: 'unit',
          label: 'Unit',
          type: 'dotnet',
          status: 'ok',
          summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
          durationMs: 1,
          groups: [{ name: 'G', summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 }, tests: [{ name: 'a', status: 'passed', durationMs: 1 }] }],
        },
      ],
    })
    expect(md).toContain('✅ **1 passed**')
    expect(md).not.toContain('Failures')
    expect(md).not.toContain('Suites without results')
  })

  it('keeps pipes and backtick runs from breaking the table or the fence', () => {
    const md = renderMarkdown({
      generatedAt: 'T',
      summary: { total: 1, passed: 0, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
      suites: [
        {
          id: 'unit',
          label: 'Uni|t',
          type: 'dotnet',
          status: 'ok',
          summary: { total: 1, passed: 0, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
          durationMs: 1,
          groups: [
            {
              name: 'G',
              summary: { total: 1, passed: 0, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
              tests: [{ name: 'a', status: 'failed', durationMs: 1, errorMessage: 'expected ```x``` got y' }],
            },
          ],
        },
      ],
    })
    expect(md).toContain('| ❌ Uni\\|t |')
    expect(md).toContain('````\nexpected ```x``` got y\n````')
  })

  it('drops failure detail rather than exceeding the job-summary size limit', () => {
    const tests = Array.from({ length: 400 }, (_, i) => ({
      name: `t${i}`,
      status: 'failed',
      durationMs: 1,
      errorMessage: 'x'.repeat(5000),
    }))
    const md = renderMarkdown({
      generatedAt: 'T',
      summary: { total: 400, passed: 0, failed: 400, skipped: 0, cancelled: 0, timedOut: 0 },
      suites: [
        {
          id: 'unit',
          label: 'Unit',
          type: 'dotnet',
          status: 'ok',
          summary: { total: 400, passed: 0, failed: 400, skipped: 0, cancelled: 0, timedOut: 0 },
          durationMs: 1,
          groups: [{ name: 'G', summary: { total: 400, passed: 0, failed: 400, skipped: 0, cancelled: 0, timedOut: 0 }, tests }],
        },
      ],
    })
    expect(Buffer.byteLength(md, 'utf8')).toBeLessThan(1024 * 1024)
    expect(md).toContain('| ❌ Unit |')
    expect(md).toContain('360 further failures omitted')
  })
})
