import { describe, expect, it } from 'vitest'
import CreateRecipeView from '~/Pages/CreateRecipe/CreateRecipeView.Component.vue'
import { ResourceString } from '~/Components/ResourceStrings'
import { renderSsr } from '../../../support/renderSsr'

// The server renders step one and nothing else: `step` starts at 1, so steps two to five, the review
// summary and the submitted sheet are client state this suite cannot reach.
const render = () => renderSsr(CreateRecipeView, { resourceStrings: {} }, undefined, { ResourceString })

// The hero above the wizard is still Softbound — SmallHero keeps its place on the retired-token
// allowlist — so every assertion reads the markup from the progress list down.
const wizardOf = (html: string) => html.slice(html.indexOf('aria-label="Steps"'))

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

// Field prints its label through a slot, so SSR wraps the text in Vue's fragment markers.
const controlIdFor = (html: string, label: string) =>
  html.match(new RegExp(`<label for="([^"]+)" class="kcc-lbl">(?:<!--\\[-->)?(?:<span>)?${label}`))?.[1] ?? ''

describe('CreateRecipeView', () => {
  it('counts the five steps on the desk and marks the one being filled', async () => {
    const wizard = wizardOf(await render())

    expect(wizard).toContain('aria-label="Steps"')
    expect(wizard).toContain('>05<')
    expect(wizard).not.toContain('>06<')
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

  it('prints the step name on the label and keeps it in the outline', async () => {
    const wizard = wizardOf(await render())
    const label = wizard.match(/<span class="kcc-label">[\s\S]*?<\/span>/)?.[0] ?? ''

    expect(label).toContain('fa-duotone fa-pen-to-square')
    expect(label).toContain('Recipe Basics')
    expect(wizard).toMatch(/<div class="kcc-sheet"[^>]*>(?:<!--[^>]*-->)*<h2 class="sr-only">Recipe Basics<\/h2>/)
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
