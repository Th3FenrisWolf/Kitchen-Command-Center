import { describe, it, expect } from 'vitest'
import { emptySummary, escapeHtml, fmtDuration, pickNewestByMtime, computeExitCode } from '../../../scripts/util.mjs'

describe('util', () => {
  it('emptySummary is all-zero with the fixed keys', () => {
    expect(emptySummary()).toEqual({
      total: 0,
      passed: 0,
      failed: 0,
      skipped: 0,
      cancelled: 0,
      timedOut: 0,
    })
  })

  it('escapeHtml escapes the dangerous characters', () => {
    expect(escapeHtml(`<a href="x">&'`)).toBe('&lt;a href=&quot;x&quot;&gt;&amp;&#39;')
    expect(escapeHtml(null)).toBe('')
  })

  it('fmtDuration shows ms under a second and seconds above', () => {
    expect(fmtDuration(0)).toBe('0ms')
    expect(fmtDuration(592)).toBe('592ms')
    expect(fmtDuration(37764)).toBe('37.8s')
    expect(fmtDuration(undefined)).toBe('—')
    expect(fmtDuration(null)).toBe('—')
    expect(fmtDuration(NaN)).toBe('—')
  })

  it('pickNewestByMtime returns the path with the greatest mtimeMs', () => {
    expect(
      pickNewestByMtime([
        { path: 'a', mtimeMs: 10 },
        { path: 'b', mtimeMs: 30 },
        { path: 'c', mtimeMs: 20 },
      ]),
    ).toBe('b')
    expect(pickNewestByMtime([{ path: 'x', mtimeMs: 5 }])).toBe('x')
    expect(pickNewestByMtime([])).toBeNull()
  })

  it('computeExitCode is non-zero on failures or non-ok suites', () => {
    const ok = { summary: { failed: 0 }, suites: [{ status: 'ok' }] }
    const failed = { summary: { failed: 2 }, suites: [{ status: 'ok' }] }
    const missing = { summary: { failed: 0 }, suites: [{ status: 'missing' }] }
    expect(computeExitCode(ok)).toBe(0)
    expect(computeExitCode(failed)).toBe(1)
    expect(computeExitCode(missing)).toBe(1)
  })
})
