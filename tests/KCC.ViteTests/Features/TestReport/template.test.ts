import { describe, it, expect } from 'vitest'
import { renderHtml } from '../../../scripts/template.mjs'

const report = {
  generatedAt: '2026-06-13T15:05:30.000Z',
  summary: { total: 3, passed: 2, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
  suites: [
    {
      id: 'unit',
      label: 'Unit',
      type: 'dotnet',
      status: 'ok',
      summary: { total: 2, passed: 1, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
      durationMs: 592,
      meta: { framework: 'TUnit' },
      groups: [
        {
          name: 'AccountViewModelTests',
          namespace: 'KCC.UnitTests',
          summary: { total: 2, passed: 1, failed: 1, skipped: 0, cancelled: 0, timedOut: 0 },
          tests: [
            { name: 'Passes', status: 'passed', durationMs: 1 },
            { name: 'Fails', status: 'failed', durationMs: 5, filePath: 'A.cs', lineNumber: 20, errorMessage: 'boom <x>' },
          ],
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

describe('renderHtml', () => {
  it('renders a self-contained tabbed document', () => {
    const html = renderHtml(report)
    expect(html.startsWith('<!DOCTYPE html>')).toBe(true)
    expect(html).toContain('data-tab="overview"')
    expect(html).toContain('data-tab="suite-unit"')
    expect(html).toContain('data-tab="suite-admin-frontend"')
    expect(html).toContain('AccountViewModelTests')
  })

  it('escapes error text and shows the missing-suite notice', () => {
    const html = renderHtml(report)
    expect(html).toContain('boom &lt;x&gt;')
    expect(html).not.toContain('boom <x>')
    expect(html).toContain('No results found')
  })

  it('embeds the report json without breaking the script tag', () => {
    const html = renderHtml(report)
    expect(html).toContain('id="report-data"')
    expect(html).not.toContain('</script><')
    expect(html).toContain('\\u003c') // < is escaped inside embedded json
  })

  it('renders groups as collapsible details with a pass/fail/skip summary', () => {
    const r = {
      generatedAt: 'T',
      summary: { total: 3, passed: 1, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
      suites: [
        {
          id: 'unit',
          label: 'Unit',
          type: 'dotnet',
          status: 'ok',
          summary: { total: 3, passed: 1, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
          durationMs: 1,
          groups: [
            {
              name: 'Passing',
              summary: { total: 1, passed: 1, failed: 0, skipped: 0, cancelled: 0, timedOut: 0 },
              tests: [{ name: 'a', status: 'passed', durationMs: 1 }],
            },
            {
              name: 'HasFailure',
              summary: { total: 2, passed: 0, failed: 1, skipped: 1, cancelled: 0, timedOut: 0 },
              tests: [
                { name: 'b', status: 'failed', durationMs: 1, errorMessage: 'x' },
                { name: 'c', status: 'skipped', durationMs: 0 },
              ],
            },
          ],
        },
      ],
    }
    const html = renderHtml(r)
    expect(html).toContain('<details class="group"') // groups are collapsible
    expect(html).toContain('<summary class="ghead">') // the header is the summary
    expect(html).toContain('class="gsum"') // per-group pass/fail/skip summary
    // a group with a failure is expanded by default; an all-passing group stays collapsed
    expect(html).toContain('<details class="group" open>')
    expect(html).toContain('<details class="group"><summary')
  })
})
