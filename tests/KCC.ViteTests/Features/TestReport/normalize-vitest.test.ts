import { describe, it, expect } from 'vitest'
import { normalizeVitest, mapVitestStatus } from '../../../scripts/normalize.mjs'

const descriptor = { id: 'web-frontend', label: 'Web Frontend' }
const repoRoot = 'C:/Repos/Kitchen-Command-Center'

const json = {
  numTotalTests: 3,
  numPassedTests: 1,
  numFailedTests: 1,
  numPendingTests: 1,
  numTodoTests: 0,
  startTime: 1000,
  testResults: [
    {
      name: 'C:/Repos/Kitchen-Command-Center/tests/KCC.ViteTests/Features/Utilities/Api.test.ts',
      startTime: 1000,
      endTime: 1120,
      status: 'failed',
      assertionResults: [
        { ancestorTitles: ['Api'], title: 'ok', status: 'passed', duration: 5, failureMessages: [] },
        { ancestorTitles: ['Api'], title: 'bad', status: 'failed', duration: 7, failureMessages: ['expected 1', '  at x'] },
        { ancestorTitles: ['Api'], title: 'later', status: 'skipped', duration: 0, failureMessages: [] },
      ],
    },
  ],
}

describe('normalizeVitest', () => {
  it('maps statuses (pending/todo/skipped -> skipped)', () => {
    expect(mapVitestStatus('passed')).toBe('passed')
    expect(mapVitestStatus('failed')).toBe('failed')
    expect(mapVitestStatus('pending')).toBe('skipped')
    expect(mapVitestStatus('todo')).toBe('skipped')
  })

  it('builds a vitest suite grouped by ancestorTitles[0]', () => {
    const suite = normalizeVitest(json, descriptor, repoRoot)
    expect(suite.type).toBe('vitest')
    expect(suite.summary).toMatchObject({ total: 3, passed: 1, failed: 1, skipped: 1 })
    expect(suite.durationMs).toBe(120) // 1120 - 1000
    expect(suite.groups).toHaveLength(1)
    expect(suite.groups[0].name).toBe('Api')
    const fail = suite.groups[0].tests.find((t) => t.name === 'bad')
    expect(fail).toMatchObject({ status: 'failed', errorMessage: 'expected 1\n  at x' })
    expect(fail!.filePath).toBe('tests/KCC.ViteTests/Features/Utilities/Api.test.ts') // repo-relative
    expect(fail!.lineNumber).toBeUndefined() // vitest json has no per-test location
  })
})
