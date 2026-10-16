import { describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import SubmittedSheet from '~/Components/RecipeWizard/SubmittedSheet.vue'
import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
import { renderSsr, tagWith } from '../../../support/ssr'

const STRINGS = {
  'RecipeWizard.RecipeSubmitted': 'Recipe Submitted!',
  'RecipeWizard.RecipeSubmittedMessage': 'Your recipe has been submitted for review. An admin will review and publish it.',
  'RecipeWizard.VariantSubmitted': 'Variant submitted!',
  'RecipeWizard.VariantSubmittedMessage': 'Your variant has been submitted for review.',
  'RecipeWizard.BackToRecipes': 'Back to Recipes',
}

const render = (props: Record<string, unknown>) =>
  renderSsr(
    defineComponent({
      setup() {
        provideResourceStrings(STRINGS, 'RecipeWizard')
        return () => h(SubmittedSheet, props)
      },
    }),
    undefined,
    undefined,
    { ResourceString },
  )

const closingLink = (html: string) => html.match(/<a[^>]*data-testid="wizard-closing-link"[^>]*>[\s\S]*?<\/a>/)?.[0] ?? ''

describe('SubmittedSheet', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render({ mode: 'recipe', recipesUrl: '/our-recipes/' })

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'data-testid="wizard-submitted"')).toContain('kcc-slip kcc-slip--wide kcc-tear-2')
  })

  it('carries no tape', async () => {
    const html = await render({ mode: 'recipe', recipesUrl: '/our-recipes/' })

    expect(html).toContain('data-testid="wizard-submitted"')
    expect(html).not.toContain('kcc-tape')
  })

  it('carries the wizard-submitted hook', async () => {
    expect(await render({ mode: 'recipe', recipesUrl: '/our-recipes/' })).toContain('data-testid="wizard-submitted"')
  })

  it('pools its green wash in the bottom-right corner, clear of the centred copy', async () => {
    const wash = tagWith(await render({ mode: 'recipe', recipesUrl: '/our-recipes/' }), 'class="kcc-wash"')

    expect(wash).toContain('--c:var(--color-green);--x:100%;--y:100%;--w:30%;--h:40%;')
  })

  it('tells the member a recipe went in for review', async () => {
    const html = await render({ mode: 'recipe', recipesUrl: '/our-recipes/' })

    expect(html).toContain('Recipe Submitted!')
    expect(html).toContain('Your recipe has been submitted for review. An admin will review and publish it.')
    expect(html).not.toContain('Variant submitted!')
  })

  it('tells the member a variant went in for review', async () => {
    const html = await render({ mode: 'variant', recipesUrl: '/our-recipes/' })

    expect(html).toContain('Variant submitted!')
    expect(html).toContain('Your variant has been submitted for review.')
    expect(html).not.toContain('Recipe Submitted!')
  })

  it('leads back to the recipe listing at the URL the page passes', async () => {
    const link = closingLink(await render({ mode: 'recipe', recipesUrl: '/our-recipes/' }))

    expect(tagWith(link, 'data-testid="wizard-closing-link"')).toContain('href="/our-recipes/"')
    expect(link).toContain('Back to Recipes')
  })

  it('takes the label and href of a closing link the page passes instead', async () => {
    const html = await render({
      mode: 'variant',
      recipesUrl: '/our-recipes/',
      closingLink: { label: 'Back to Brown Butter Gnocchi', href: '/recipes/gnocchi' },
    })
    const link = closingLink(html)

    expect(tagWith(link, 'data-testid="wizard-closing-link"')).toContain('href="/recipes/gnocchi"')
    expect(link).toContain('Back to Brown Butter Gnocchi')
    expect(html).not.toContain('Back to Recipes')
    expect(html).not.toContain('/our-recipes/')
  })
})
