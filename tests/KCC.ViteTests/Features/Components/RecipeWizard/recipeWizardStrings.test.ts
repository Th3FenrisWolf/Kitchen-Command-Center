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

const detailsStepStrings: [string, string][] = [
  ['Details', 'Details'],
  ['DetailsHint', 'All optional. The owner can fill in anything you leave out.'],
  ['Category', 'Category'],
  ['NoCategory', 'No category'],
  ['DietTags', 'Diet'],
  ['StyleTags', 'Style'],
  ['Difficulty', 'Difficulty'],
  ['DifficultyNone', 'Not set'],
  ['DifficultyEasy', 'Easy'],
  ['DifficultyMedium', 'Medium'],
  ['DifficultyHard', 'Hard'],
  ['NutritionPerServing', 'Nutrition per serving'],
  ['Calories', 'Calories'],
  ['Protein', 'Protein'],
  ['Carbohydrates', 'Carbohydrates'],
  ['Fat', 'Fat'],
  ['SaturatedFat', 'Saturated fat'],
  ['Fiber', 'Fiber'],
  ['Sugar', 'Sugar'],
  ['Sodium', 'Sodium'],
  ['Kcal', 'kcal'],
  ['Grams', 'g'],
  ['Milligrams', 'mg'],
  ['NutritionInvalid', 'Enter a whole number from 0 to {0}.'],
  ['ProposedCategory', 'Suggest a new category'],
  ['ProposedTags', 'Suggest new tags'],
  ['ProposalHint', 'The owner sees this when reviewing your recipe. It never shows on the site.'],
  ['ReviewDetails', 'Details'],
  ['NoDetailsGiven', 'No details given.'],
  [
    'DetailsChanged',
    "The site's categories or tags changed while you were working. Check your choices on the Details step and submit again.",
  ],
  ['MarkSuggested', 'Suggested by the reader'],
  ['MarkCalculated', 'Calculated'],
  ['MarkReasonPrintedAs', 'Printed as {0}'],
]

const englishOf = (xml: string) => /<Translation Language="en-US">([\s\S]*?)<\/Translation>/.exec(xml)?.[1]

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

  it('lists the 33 Details step strings with their English', () => {
    const missing = detailsStepStrings.filter(([key]) => !keys.includes(key)).map(([key]) => key)
    const reworded = detailsStepStrings
      .filter(([key, english]) => englishOf(dictionaryItem(key)) !== english)
      .map(([key]) => key)

    expect(detailsStepStrings).toHaveLength(33)
    expect(missing).toEqual([])
    expect(reworded).toEqual([])
  })
})
