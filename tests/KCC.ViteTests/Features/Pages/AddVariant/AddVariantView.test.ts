import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import AddVariantView from '~/Pages/AddVariant/AddVariantView.Component.vue'
import { renderSsr } from '../../../support/renderSsr'

// Only the keys step one composes; the rest fall back to their key, as they do in the app.
const STRINGS = {
  'AddVariant.AddVariantFor': 'Add a variant of',
  'AddVariant.Cancel': 'Cancel',
  'AddVariant.CookTime': 'Cook time',
  'AddVariant.Description': 'Description',
  'AddVariant.DescriptionPlaceholder': 'What makes this one different?',
  'AddVariant.Min': 'min',
  'AddVariant.Next': 'Next',
  'AddVariant.PrepTime': 'Prep time',
  'AddVariant.Servings': 'Servings',
  'AddVariant.VariantInfo': 'Variant info',
  'AddVariant.VariantName': 'Variant name',
}

// The server renders step one and nothing else: `step` starts at 1, so the ingredients, the method, the
// review summary and the submitted sheet are client state this suite cannot reach.
const render = () =>
  renderSsr(AddVariantView, {
    recipeId: '11111111-2222-3333-4444-555555555555',
    recipeName: 'Brown Butter Gnocchi',
    recipeSlug: '~/recipes/gnocchi',
    resourceStrings: STRINGS,
  })

// The hero above the wizard is its own sheet, with its own suite; every assertion about the wizard reads
// the markup from the progress list down.
const wizardOf = (html: string) => html.slice(html.indexOf('aria-label="Steps"'))

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

// Field prints its label through a slot, so SSR wraps the text in Vue's fragment markers.
const controlIdFor = (html: string, label: string) =>
  html.match(new RegExp(`<label for="([^"]+)" class="kcc-lbl">(?:<!--\\[-->)?(?:<span>)?${label}`))?.[1] ?? ''

describe('AddVariantView', () => {
  // Nothing above the hero supplies a margin: without its own the sheet and its tape sit flush under the
  // header's dashed rule.
  it('sets the hero a rule below the header', async () => {
    expect(tagWith(await render(), 'kcc-tear-hero')).toContain('mt-6')
  })

  it('counts the four steps on the desk and marks the one being filled', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('aria-label="Steps"')
    expect(wizard).toContain('>04<')
    expect(wizard).not.toContain('>05<')
    expect(tagWith(wizard, 'aria-current="step"')).toBeTruthy()
  })

  it('sets the step on one crisp sheet, the fall outside the tear', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('<div class="kcc-slip kcc-tear-1" style="--r:0;">')
    expect(wizard).toContain('<div class="kcc-torn"><div class="kcc-sheet"')
    expect(wizard.match(/kcc-slip/g)).toHaveLength(1)
  })

  it('pads the sheet up to 48px without a media query', async () => {
    const wizard = wizardOf(await render())

    expect(tagWith(wizard, 'kcc-sheet')).toContain('--pad:clamp(24px, 7.5vw, 48px)')
  })

  it('prints the step name on the label, editable, and keeps it in the outline', async () => {
    const wizard = wizardOf(await render())
    const label = wizard.match(/<span class="kcc-label">[\s\S]*?<\/span>/)?.[0] ?? ''

    expect(label).toContain('fa-duotone fa-pen-to-square')
    expect(label).toContain('<span>Variant info</span>')
    expect(wizard).toMatch(/<div class="kcc-sheet"[^>]*>(?:<!--[^>]*-->)*<h2 class="sr-only">Variant info<\/h2>/)
  })

  it('binds each visible label to the control it names', async () => {
    const wizard = wizardOf(await render())
    const nameId = controlIdFor(wizard, 'Variant name')
    const descriptionId = controlIdFor(wizard, 'Description')
    const prepId = controlIdFor(wizard, 'Prep time')

    expect(tagWith(wizard, `id="${nameId}"`)).toMatch(/^<input /)
    expect(tagWith(wizard, `id="${descriptionId}"`)).toMatch(/^<textarea /)
    expect(tagWith(wizard, `id="${prepId}"`)).toMatch(/^<input /)
    expect(new Set([nameId, descriptionId, prepId]).size).toBe(3)
    expect(wizard).toContain('<span aria-hidden="true"> *</span>')
  })

  it('takes the field label from a ResourceString, not a bare prop', async () => {
    const wizard = wizardOf(await render())

    // `data-resource-key`/`kcc-rs-editable` (ResourceString.Component.vue) only render in preview mode,
    // which this suite doesn't inject, so they can't be the signal here. What SSR without preview can
    // show is the wrapping element ResourceString always renders (`as` defaults to `span`): a `label`
    // string prop interpolates as bare text with no wrapper, so the `<span>` proves the field went
    // through the slot.
    expect(wizard).toMatch(/<label for="[^"]+" class="kcc-lbl">(?:<!--\[-->)?<span>Variant name<\/span>/)
  })

  it('leaves the stepper its own accessible names beside the visible label', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('aria-label="Prep time in min"')
    expect(wizard).toContain('aria-label="Increase Prep time"')
    expect(wizard).toContain('aria-label="Decrease Cook time"')
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

  it('leaves the recipe by a hairline pill with the tilde stripped', async () => {
    const cancel = tagWith(await render(), 'href="/recipes/gnocchi"')

    expect(cancel).toMatch(/^<a /)
    expect(cancel).toContain('kcc-btn kcc-btn--ghost')
    expect(cancel).not.toMatch(/\brounded-/)
  })

  it('keeps the later steps and the submitted sheet off the server', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).not.toContain('fa-clipboard-check')
    expect(wizard).not.toContain('kcc-tape')
    expect(wizard).not.toContain('kcc-wash')
  })

  it('leaves no Softbound remnant on the wizard', async () => {
    const wizard = wizardOf(await render())
    const sheet = wizard.slice(wizard.indexOf('<div class="kcc-slip'))

    expect(wizard).not.toMatch(/sk-[a-z]/)
    expect(wizard).not.toMatch(/\bv-ink\b/)
    expect(wizard).not.toMatch(/\brounded-(?:lg|xl|2xl|3xl)\b/)
    expect(wizard).not.toMatch(/\bfont-(?:bold|semibold|medium)\b/)
    expect(wizard).not.toMatch(/\bshadow-/)
    expect(wizard).not.toMatch(/\btext-(?:danger|warning|success|rating)-ink\b/)
    expect(sheet).not.toMatch(/\bbg-(?:paper|paper-2|marker)\b/)
    expect(sheet).not.toMatch(/\btext-lg\b/)
  })
})
