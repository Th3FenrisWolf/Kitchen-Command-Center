import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { navLabels } from '~/Components/Header/navLabels'
import { files } from '../../../support/files'

const WEB = fileURLToPath(new URL('../../../../../src/KCC.Web/', import.meta.url))

// A key with no dictionary item renders as its own name, so the pad's labels are checked against the items themselves.
const used = new Set<string>()
for (const file of files(`${WEB}Features/Components/Header`, /\.(vue|ts)$/)) {
  for (const [, key] of readFileSync(file, 'utf8').matchAll(/\bt(?:\.value)?\('([A-Za-z]+)'/g)) {
    used.add(key!)
  }
}

describe('navLabels', () => {
  it('fills in each placeholder', () => {
    const t = navLabels({ 'Nav.AllResults': 'All {0} results', 'Nav.KitchenOf': '{0}’s kitchen' })

    expect(t('AllResults', 7)).toBe('All 7 results')
    expect(t('KitchenOf', 'Grace')).toBe('Grace’s kitchen')
  })

  it('falls back to the key, as every resource string does', () => {
    expect(navLabels({})('MyKitchen')).toBe('Nav.MyKitchen')
  })

  it('finds the labels the pad uses', () => {
    expect(used.size).toBeGreaterThan(20)
  })

  it('asks only for labels the Nav dictionary group holds', () => {
    const missing = [...used].filter((key) => !existsSync(`${WEB}uSync/v17/Dictionary/nav.${key.toLowerCase()}.config`))

    expect(missing).toEqual([])
  })
})
