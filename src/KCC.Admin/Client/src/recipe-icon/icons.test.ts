import { describe, expect, it } from 'vitest'
import { filterIcons, iconLabel } from './icons.js'

const icons = ['fa-duotone fa-cheese', 'fa-duotone fa-cheese-swiss', 'fa-duotone fa-egg']

describe('iconLabel', () => {
  it('drops the style and prefix from a class string', () => {
    expect(iconLabel('fa-duotone fa-cheese-swiss')).toBe('cheese-swiss')
  })
})

describe('filterIcons', () => {
  it('keeps every icon for a blank search', () => {
    expect(filterIcons(icons, '  ')).toEqual(icons)
  })

  it('matches the label, ignoring case and the style prefix', () => {
    expect(filterIcons(icons, 'CHEESE')).toEqual(['fa-duotone fa-cheese', 'fa-duotone fa-cheese-swiss'])
    expect(filterIcons(icons, 'duotone')).toEqual([])
  })
})
