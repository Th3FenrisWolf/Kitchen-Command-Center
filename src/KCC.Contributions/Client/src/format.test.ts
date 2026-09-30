import { describe, expect, it } from 'vitest'
import { clampPage, formatDateTime, formatRating, ratingSteps, totalPagesFor } from './format.js'

describe('formatDateTime', () => {
  it('writes nothing for a missing or unreadable date', () => {
    expect(formatDateTime(null)).toBe('')
    expect(formatDateTime('not-a-date')).toBe('')
  })

  it('reads a timestamp without an offset as UTC', () => {
    expect(formatDateTime('2026-09-25T15:30:00')).toBe(formatDateTime('2026-09-25T15:30:00Z'))
    expect(formatDateTime('2026-09-25T15:30:00Z')).not.toBe('')
  })
})

describe('formatRating', () => {
  it('writes one decimal and a star', () => {
    expect(formatRating(4.5)).toBe('4.5 ★')
    expect(formatRating(3)).toBe('3.0 ★')
  })
})

describe('ratingSteps', () => {
  it('runs from half a star to five in half steps', () => {
    expect(ratingSteps).toHaveLength(10)
    expect(ratingSteps[0]).toBe(0.5)
    expect(ratingSteps.at(-1)).toBe(5)
  })
})

describe('totalPagesFor', () => {
  it('always reports at least one page and rounds a part page up', () => {
    expect(totalPagesFor(0, 20)).toBe(1)
    expect(totalPagesFor(21, 20)).toBe(2)
    expect(totalPagesFor(40, 20)).toBe(2)
  })
})

describe('clampPage', () => {
  it('keeps a page that exists', () => {
    expect(clampPage(1, 40, 20)).toBe(1)
    expect(clampPage(2, 40, 20)).toBe(2)
  })

  it('moves a page past the end back to the last page', () => {
    expect(clampPage(3, 40, 20)).toBe(2)
    expect(clampPage(2, 20, 20)).toBe(1)
    expect(clampPage(4, 61, 20)).toBe(4)
    expect(clampPage(5, 61, 20)).toBe(4)
  })

  it('never goes below the first page, even when nothing is left', () => {
    expect(clampPage(2, 0, 20)).toBe(1)
    expect(clampPage(0, 40, 20)).toBe(1)
    expect(clampPage(-3, 40, 20)).toBe(1)
  })
})
