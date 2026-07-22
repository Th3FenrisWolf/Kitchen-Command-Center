import { describe, expect, it } from 'vitest'
import {
  MAX_TIME,
  defaultState,
  buildSearchParams,
  chipsFor,
  activeFilterCount,
  timeRangeLabel,
  type RecipeSearchState,
} from '~/Pages/RecipeSearch/recipeSearchCriteria'

const state = (over: Partial<RecipeSearchState> = {}): RecipeSearchState => ({ ...defaultState(), ...over })

// Stand-in for the resource-string resolver the Vue tree provides at runtime.
const t = (key: string): string => {
  const strings: Record<string, string> = { Min: 'min', OrMore: 'or more', OrLess: 'or less' }
  return strings[key] ?? key
}

describe('buildSearchParams', () => {
  it('serializes query, repeated facets, sort and paging', () => {
    const p = buildSearchParams(state({ query: 'chicken', categories: ['Mains'], diets: ['Vegan', 'Dairy-Free'], sort: 'rated' }), 2, 12)
    expect(p.get('q')).toBe('chicken')
    expect(p.getAll('category')).toEqual(['Mains'])
    expect(p.getAll('diet')).toEqual(['Vegan', 'Dairy-Free'])
    expect(p.get('sort')).toBe('rated')
    expect(p.get('page')).toBe('2')
    expect(p.get('pageSize')).toBe('12')
  })

  it('omits the time range when it is the default (Any)', () => {
    const p = buildSearchParams(state(), 0, 12)
    expect(p.has('timeMin')).toBe(false)
    expect(p.has('timeMax')).toBe(false)
  })

  it('includes the time range when narrowed', () => {
    const p = buildSearchParams(state({ timeMin: 10, timeMax: 30 }), 0, 12)
    expect(p.get('timeMin')).toBe('10')
    expect(p.get('timeMax')).toBe('30')
  })
})

describe('chipsFor', () => {
  it('produces a chip per active filter', () => {
    const chips = chipsFor(state({ query: 'cake', categories: ['Cookies'], diets: ['Vegan'], timeMin: 5, timeMax: 60 }), t)
    expect(chips.map((c) => c.label)).toEqual(['“cake”', 'Cookies', 'Vegan', '5 min or more'])
  })

  it('is empty with no active filters', () => {
    expect(chipsFor(state(), t)).toEqual([])
  })
})

describe('activeFilterCount', () => {
  it('counts categories + diets + a narrowed time as one', () => {
    expect(activeFilterCount(state({ categories: ['a', 'b'], diets: ['c'], timeMin: 5 }))).toBe(4)
    expect(activeFilterCount(state())).toBe(0)
  })
})

describe('timeRangeLabel', () => {
  it('labels a bounded range and the no-filter default', () => {
    expect(timeRangeLabel(0, MAX_TIME, t)).toBe('Any')
    expect(timeRangeLabel(10, 30, t)).toBe('10–30 min')
  })

  it('collapses an open-ended top to "min or more"', () => {
    expect(timeRangeLabel(15, MAX_TIME, t)).toBe('15 min or more')
    expect(timeRangeLabel(55, MAX_TIME, t)).toBe('55 min or more')
  })

  it('collapses an open-ended bottom to "min or less"', () => {
    expect(timeRangeLabel(0, 40, t)).toBe('40 min or less')
  })
})
