import { existsSync, readFileSync } from 'node:fs'
import { basename } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { recipeWizardKeys } from '~/Components/RecipeWizard/recipeWizardKeys'
import { files } from '../../../support/files'

const WEB = fileURLToPath(new URL('../../../../../src/KCC.Web/', import.meta.url))

const keys = Object.keys(recipeWizardKeys)

const sources = [...files(`${WEB}Features/Components/RecipeWizard`, /\.(vue|ts)$/)]
  .filter((file) => basename(file) !== 'recipeWizardKeys.ts')
  .map((file) => readFileSync(file, 'utf8'))

const matches = (pattern: RegExp) => new Set(sources.flatMap((source) => [...source.matchAll(pattern)].map((m) => m[1]!)))

const literalKeys = new Set([
  ...matches(/\brs\(\s*['"](\w+)['"]/g),
  ...matches(/<ResourceString\b(?![^>]*\sshared\b)[^>]*?\sfor="(\w+)"/g),
])
const keysThroughTheMap = matches(/\brecipeWizardKeys\.(\w+)/g)

const dictionaryItem = (key: string) => {
  const file = `${WEB}uSync/v17/Dictionary/recipewizard.${key.toLowerCase()}.config`
  return existsSync(file) ? readFileSync(file, 'utf8') : ''
}

const isRecipeWizardItem = (key: string, xml: string) =>
  xml.includes(`Alias="RecipeWizard.${key}"`) &&
  xml.includes('<Parent>RecipeWizard</Parent>') &&
  /<Translation Language="en-US">\s*\S[\s\S]*?<\/Translation>/.test(xml)

describe('RecipeWizard strings', () => {
  it('maps each key to itself, for a resolver that adds the group', () => {
    expect(keys.length).toBeGreaterThan(0)
    expect(Object.entries(recipeWizardKeys).filter(([key, value]) => key !== value)).toEqual([])
  })

  it('gives every key a RecipeWizard dictionary item with an en-US value', () => {
    expect(keys.filter((key) => !isRecipeWizardItem(key, dictionaryItem(key)))).toEqual([])
  })

  it('lists every key the wizard names through rs() or ResourceString', () => {
    expect([...literalKeys].filter((key) => !keys.includes(key))).toEqual([])
  })

  it('uses every key it lists, by name or through the map', () => {
    expect(keys.filter((key) => !literalKeys.has(key) && !keysThroughTheMap.has(key))).toEqual([])
  })
})
