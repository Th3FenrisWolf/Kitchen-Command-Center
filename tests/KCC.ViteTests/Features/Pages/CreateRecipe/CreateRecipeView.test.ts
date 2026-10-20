import { describe, expect, it } from 'vitest'
import CreateRecipeView from '~/Pages/CreateRecipe/CreateRecipeView.Component.vue'
import { ResourceString } from '~/Components/ResourceStrings'
import { renderSsr, tagWith, expectNoRetiredMarkup } from '../../../support/ssr'

// Only the keys the hero and step one compose.
const STRINGS = {
  'CreateRecipe.CreateRecipe': 'Create Recipe',
  'RecipeWizard.RecipeBasics': 'Recipe Basics',
  'RecipeWizard.RecipeName': 'Recipe Name',
  'RecipeWizard.RecipeNamePlaceholder': 'e.g., Mac & Cheese',
  'RecipeWizard.Description': 'Description',
  'RecipeWizard.RecipeDescriptionPlaceholder': 'A short description of this dish',
  'RecipeWizard.ProgressLabel': 'Steps',
  'RecipeWizard.Next': 'Next',
}

// The server renders step one and nothing else, so steps two to five, the review summary and the submitted
// sheet are client state this suite cannot reach.
const render = () =>
  renderSsr(CreateRecipeView, { resourceStrings: STRINGS, recipesUrl: '/recipes/' }, undefined, { ResourceString })

// The hero above the wizard is its own sheet, with its own suite; every assertion below reads the markup
// from the progress list down.
const wizardOf = (html: string) => html.slice(html.indexOf('aria-label="Steps"'))

// Field prints its label through a slot, so SSR wraps the text in Vue's fragment markers.
const controlIdFor = (html: string, label: string) =>
  html.match(new RegExp(`<label for="([^"]+)" class="kcc-lbl">(?:<!--\\[-->)?(?:<span>)?${label}`))?.[1] ?? ''

describe('CreateRecipeView', () => {
  it('rests the hero and the step each on one blank, with the wide turn', async () => {
    const html = await render()
    const hero = html.slice(0, html.indexOf('aria-label="Steps"'))
    const wizard = wizardOf(html)

    expect((hero.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(hero, 'kcc-tear-hero')).toContain('kcc-slip kcc-slip--wide kcc-tear-hero')
    expect((wizard.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(wizard, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-1')
  })

  // Nothing above the hero supplies a margin: without its own the sheet sits flush under the header's dashed
  // rule.
  it('sets the hero a rule below the header', async () => {
    expect(tagWith(await render(), 'kcc-tear-hero')).toContain('mt-rule-1')
  })

  it('titles the hero from its own group and the wizard from the shared one', async () => {
    const html = await render()

    expect(html.slice(0, html.indexOf('aria-label="Steps"'))).toContain('Create Recipe')
    expect(html.match(/\b(?:RecipeWizard|CreateRecipe)\.\w+/g) ?? []).toEqual([])
  })

  it('counts the five steps on the desk and marks the one being filled', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('aria-label="Steps"')
    expect(wizard).toContain('>05<')
    expect(wizard).not.toContain('>06<')
    expect(tagWith(wizard, 'aria-current="step"')).toBeTruthy()
  })

  it('sets the step on one crisp sheet, the fall outside the tear', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('<div class="kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-1" style="--r:0;">')
    expect(wizard).toContain('<div class="kcc-torn"><div class="kcc-sheet kcc-sheet--margin"')
    expect(wizard.match(/class="kcc-slip /g)).toHaveLength(1)
  })

  it('pads the sheet up to 48px without a media query', async () => {
    const wizard = wizardOf(await render())

    expect(tagWith(wizard, 'kcc-sheet')).toContain('--pad:clamp(24px, 7.5vw, 48px)')
  })

  it('prints the step name on the label and keeps it in the outline', async () => {
    const wizard = wizardOf(await render())
    const label = wizard.match(/<span class="kcc-label">[\s\S]*?<\/span>/)?.[0] ?? ''

    expect(label).toContain('fa-duotone fa-pen-to-square')
    expect(label).toContain('Recipe Basics')
    expect(wizard).toMatch(
      /<div class="kcc-sheet kcc-sheet--margin"[^>]*>(?:<!--[^>]*-->)*<h2 class="sr-only">Recipe Basics<\/h2>/,
    )
  })

  it('binds each visible label to the control it names', async () => {
    const wizard = wizardOf(await render())
    const nameId = controlIdFor(wizard, 'Recipe Name')
    const descriptionId = controlIdFor(wizard, 'Description')

    expect(nameId).toBeTruthy()
    expect(descriptionId).toBeTruthy()
    expect(nameId).not.toEqual(descriptionId)
    expect(tagWith(wizard, `id="${nameId}"`)).toMatch(/^<input /)
    expect(tagWith(wizard, `id="${descriptionId}"`)).toMatch(/^<textarea /)
    expect(wizard).toContain('<span aria-hidden="true"> *</span>')
    expect(wizard).toContain('kcc-field')
  })

  it('advances on submit from a marker pill, guarded until the step is filled', async () => {
    const wizard = wizardOf(await render())
    const submit = tagWith(wizard, 'type="submit"')

    expect(wizard).toContain('<form')
    expect(submit).toMatch(/^<button /)
    expect(submit).toContain('class="kcc-btn"')
    expect(submit).toContain('disabled')
    expect(wizard).not.toContain('fa-arrow-left')
  })

  it('keeps the later steps and the submitted sheet off the server', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).not.toContain('fa-clipboard-check')
    expect(wizard).not.toContain('kcc-tape')
    expect(wizard).not.toContain('kcc-wash')
  })

  it('opens the wizard on the start step when the page turns the photo import on', async () => {
    const html = await renderSsr(
      CreateRecipeView,
      {
        resourceStrings: STRINGS,
        recipesUrl: '/recipes/',
        photoImport: { enabled: true, available: true, maxImageEdge: 2000 },
      },
      undefined,
      { ResourceString },
    )

    expect(tagWith(html, 'data-testid="wizard-start-photos"')).toMatch(/^<button /)
    expect(html).not.toContain('aria-current="step"')
  })

  it('leaves no Softbound remnant on the wizard', async () => {
    const wizard = wizardOf(await render())
    const sheet = wizard.slice(wizard.indexOf('<div class="kcc-slip'))

    expectNoRetiredMarkup(wizard)
    expect(sheet).not.toMatch(/\bbg-(?:paper|paper-2|marker)\b/)
    expect(sheet).not.toMatch(/\btext-lg\b/)
  })
})
