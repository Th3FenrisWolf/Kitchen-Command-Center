// @vitest-environment happy-dom
import { createApp, nextTick } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'
import RecipeWizard from '~/Components/RecipeWizard/RecipeWizard.vue'
import { ResourceString } from '~/Components/ResourceStrings'
import { stepFlips } from '~/Components/RecipeWizard/stepFlip'
import type { DetailsChoices } from '~/Components/RecipeWizard/wizardDetails'
import { post } from '~/Utilities/Api'
import { wizardStrings } from '../../../support/recipeWizard'
import { renderSsr, tagWith } from '../../../support/ssr'

vi.mock('~/Utilities/Api', async (importOriginal) => ({ ...(await importOriginal<object>()), post: vi.fn() }))

const KEYS = [
  'RecipeBasics',
  'RecipeName',
  'RecipeNamePlaceholder',
  'Description',
  'RecipeDescriptionPlaceholder',
  'FirstVariant',
  'FirstVariantHint',
  'VariantInfo',
  'VariantName',
  'VariantNamePlaceholder',
  'VariantDescriptionPlaceholder',
  'PrepTime',
  'CookTime',
  'Servings',
  'Serves',
  'Min',
  'ValueInUnit',
  'DecreaseLabel',
  'IncreaseLabel',
  'Ingredients',
  'IngredientName',
  'IngredientNamePlaceholder',
  'Eyeball',
  'Quantity',
  'QuantityPlaceholder',
  'Unit',
  'UnitPlaceholder',
  'RemoveIngredient',
  'AddIngredient',
  'ToTaste',
  'Instructions',
  'DescribeThisStep',
  'RemoveStep',
  'AddStep',
  'ProgressLabel',
  'Next',
  'Back',
  'ReviewAndSubmit',
  'ReviewRecipe',
  'ReviewFirstVariant',
  'Step',
  'Steps',
  'SubmitForReview',
  'Submitting',
  'RecipeSubmitted',
  'RecipeSubmittedMessage',
  'VariantSubmitted',
  'VariantSubmittedMessage',
  'BackToRecipes',
  'SubmitFailed',
  'UnexpectedError',
  'SignInAgain',
  'RecipeNotFound',
  'TooManyAttempts',
]

// Each value differs from its key, so a string left unresolved shows up as its key.
const STRINGS = Object.fromEntries(KEYS.map((key) => [`RecipeWizard.${key}`, `«${key}»`]))

const GNOCCHI = { key: '11111111-2222-3333-4444-555555555555', name: 'Brown Butter Gnocchi' }

const renderRecipe = () =>
  renderSsr(RecipeWizard, { flow: 'recipe', recipesUrl: '/our-recipes/', resourceStrings: STRINGS }, undefined, {
    ResourceString,
  })

const renderVariant = () =>
  renderSsr(
    RecipeWizard,
    {
      flow: 'variant',
      recipe: GNOCCHI,
      closingLink: { label: 'Back to Brown Butter Gnocchi', href: '/recipes/gnocchi' },
      resourceStrings: STRINGS,
    },
    undefined,
    { ResourceString },
  )

const nextButton = (html: string) => html.match(/<button[^>]*type="submit"[^>]*>[\s\S]*?<\/button>/)?.[0] ?? ''

const unresolvedKeys = (html: string) => html.match(/\b(?:RecipeWizard|CreateRecipe|AddVariant|Shared)\.\w+/g) ?? []

describe('RecipeWizard in recipe mode, with the import off', () => {
  it('opens on the basics step, its recipe name and description hooked', async () => {
    const html = await renderRecipe()

    expect(html).toContain('«RecipeBasics»')
    expect(tagWith(html, 'data-testid="wizard-recipe-name"')).toMatch(/^<input /)
    expect(tagWith(html, 'data-testid="wizard-recipe-description"')).toMatch(/^<textarea /)
    expect(html).not.toContain('wizard-variant-name')
  })

  it('counts six steps under a label from its strings', async () => {
    const html = await renderRecipe()

    expect(tagWith(html, 'aria-current="step"')).toBeTruthy()
    expect(html).toContain('aria-label="«ProgressLabel»"')
    expect(html).toContain('>06<')
    expect(html).not.toContain('>07<')
  })

  it('advances from a Next button, held until the step is filled in', async () => {
    const next = nextButton(await renderRecipe())

    expect(next).toContain('«Next»')
    expect(tagWith(next, 'type="submit"')).toContain('disabled')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await renderRecipe())).toEqual([])
  })
})

describe('RecipeWizard in variant mode, with the import off', () => {
  it('opens on the variant step, its variant name and description hooked', async () => {
    const html = await renderVariant()

    expect(html).toContain('«VariantInfo»')
    expect(tagWith(html, 'data-testid="wizard-variant-name"')).toMatch(/^<input /)
    expect(tagWith(html, 'data-testid="wizard-variant-description"')).toMatch(/^<textarea /)
    expect(html).not.toContain('wizard-recipe-name')
  })

  it('keeps the ids ending -prep-time, -cook-time and -servings on the stepper inputs', async () => {
    const html = await renderVariant()

    expect(html).toMatch(/<input[^>]*\sid="[^"]+-prep-time"/)
    expect(html).toMatch(/<input[^>]*\sid="[^"]+-cook-time"/)
    expect(html).toMatch(/<input[^>]*\sid="[^"]+-servings"/)
  })

  it('counts five steps under a label from its strings', async () => {
    const html = await renderVariant()

    expect(tagWith(html, 'aria-current="step"')).toBeTruthy()
    expect(html).toContain('aria-label="«ProgressLabel»"')
    expect(html).toContain('>05<')
    expect(html).not.toContain('>06<')
  })

  it('advances from a Next button, held until the step is filled in', async () => {
    const next = nextButton(await renderVariant())

    expect(next).toContain('«Next»')
    expect(tagWith(next, 'type="submit"')).toContain('disabled')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await renderVariant())).toEqual([])
  })
})

describe('RecipeWizard with the import on', () => {
  const IMPORT_ON = { enabled: true, available: true, maxImageEdge: 2000 }

  const renderWithImport = (props: Record<string, unknown>) =>
    renderSsr(RecipeWizard, { ...props, photoImport: IMPORT_ON, resourceStrings: wizardStrings() }, undefined, {
      ResourceString,
    })

  const FLOWS = [
    ['recipe', { flow: 'recipe', recipesUrl: '/our-recipes/' }],
    ['variant', { flow: 'variant', recipe: GNOCCHI }],
  ] as const

  it.each(FLOWS)('opens the %s flow on the start step, before any step is counted', async (_flow, props) => {
    const html = await renderWithImport(props)

    expect(tagWith(html, 'data-testid="wizard-start-photos"')).toMatch(/^<button /)
    expect(tagWith(html, 'data-testid="wizard-start-typing"')).toMatch(/^<button /)
    expect(html).not.toContain('aria-current="step"')
    expect(html).not.toContain('data-testid="wizard-recipe-name"')
    expect(html).not.toContain('data-testid="wizard-variant-name"')
  })

  it.each(FLOWS)('shows no photo strip before the %s flow holds a photo', async (_flow, props) => {
    expect(await renderWithImport(props)).not.toContain('wizard-photos')
  })

  it.each(FLOWS)('prints no leave dialog on the server in the %s flow', async (_flow, props) => {
    const html = await renderWithImport(props)

    expect(html).not.toContain('leave-dialog')
    expect(html).not.toContain('role="alertdialog"')
    expect(html).not.toContain('<!--teleport')
  })

  it.each(FLOWS)('resolves every string of the %s flow under the RecipeWizard group', async (_flow, props) => {
    expect(unresolvedKeys(await renderWithImport(props))).toEqual([])
  })

  it.each(FLOWS)('rests the photo choice in the %s flow while the import is unavailable', async (_flow, props) => {
    const html = await renderSsr(
      RecipeWizard,
      {
        ...props,
        photoImport: { enabled: true, available: false, availableAgainOn: '2026-11-01', maxImageEdge: 2000 },
        resourceStrings: wizardStrings({
          ImportUnavailable: 'Photo import is resting until {0}. You can still type the recipe in.',
        }),
      },
      undefined,
      { ResourceString },
    )

    expect(html).toMatch(/data-testid="import-unavailable"[^>]*>\s*Photo import is resting until November 1, 2026\./)
    expect(tagWith(html, 'data-testid="wizard-start-photos"')).toContain('disabled')
    expect(tagWith(html, 'data-testid="wizard-start-typing"')).not.toContain('disabled')
  })
})

describe('RecipeWizard on the step the server renders', () => {
  const stepSlip = (html: string) => tagWith(html, 'class="kcc-slip ')

  const FIRST_STEPS = [
    ['the basics step of the recipe flow', renderRecipe],
    ['the variant step of the variant flow', renderVariant],
    [
      'the start step with the import on',
      () =>
        renderSsr(
          RecipeWizard,
          {
            flow: 'recipe',
            recipesUrl: '/our-recipes/',
            photoImport: { enabled: true, available: true, maxImageEdge: 2000 },
            resourceStrings: wizardStrings(),
          },
          undefined,
          { ResourceString },
        ),
    ],
  ] as const

  it.each(FIRST_STEPS)('turns %s over exactly when the first step flips', async (_step, render) => {
    const slip = stepSlip(await render())

    expect(stepFlips(1)).toBe(true)
    expect(slip).toContain('class="kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-1"')
  })
})

describe('RecipeWizard Details step', () => {
  const CHOICES: DetailsChoices = {
    categories: [{ key: 'cat-dinner', name: 'Dinner' }],
    tags: [
      { key: 'tag-spicy', name: 'Spicy', kind: 'Style' },
      { key: 'tag-vegan', name: 'Vegan', kind: 'Diet' },
    ],
  }

  const NEW_CHOICES: DetailsChoices = {
    categories: [{ key: 'cat-breakfast', name: 'Breakfast' }],
    tags: [{ key: 'tag-vegan', name: 'Vegan', kind: 'Diet' }],
  }

  const mounted: (() => void)[] = []

  afterEach(() => {
    mounted.splice(0).forEach((unmount) => unmount())
    vi.mocked(post).mockReset()
  })

  const settle = async () => {
    await nextTick()
    await new Promise((resolve) => setTimeout(resolve))
  }

  const mountVariant = (props: Record<string, unknown> = {}) => {
    const host = document.createElement('div')
    document.body.append(host)
    const app = createApp(RecipeWizard, {
      flow: 'variant',
      recipe: GNOCCHI,
      resourceStrings: wizardStrings(),
      choices: CHOICES,
      ...props,
    })
    app.component('ResourceString', ResourceString)
    app.mount(host)
    mounted.push(() => {
      app.unmount()
      host.remove()
    })
    return host
  }

  const type = async (host: HTMLElement, testid: string, text: string) => {
    const field = host.querySelector<HTMLInputElement | HTMLTextAreaElement>(`[data-testid="${testid}"]`)!
    field.value = text
    field.dispatchEvent(new Event('input'))
    await settle()
  }

  const press = async (host: HTMLElement, selector: string) => {
    host.querySelector<HTMLElement>(selector)!.click()
    await settle()
  }

  const nextStep = (host: HTMLElement) => press(host, 'button[type="submit"]')

  const openDetails = async (host: HTMLElement) => {
    await type(host, 'wizard-variant-name', 'Buttermilk')
    await nextStep(host)
    await type(host, 'wizard-ingredient-name', 'flour')
    await nextStep(host)
    await type(host, 'wizard-instruction-text', 'Whisk.')
    await nextStep(host)
  }

  it('renders the Details step after the instructions', async () => {
    const host = mountVariant()

    await openDetails(host)

    expect(host.querySelector('[data-testid="details-step"]')).not.toBeNull()
    expect(host.querySelector('[data-testid="wizard-instruction-text"]')).toBeNull()
  })

  it('hands the choices to the step', async () => {
    const host = mountVariant()

    await openDetails(host)

    expect(host.querySelector('[data-testid="details-tag-tag-spicy"]')).not.toBeNull()
    expect(host.querySelector('[data-testid="details-tag-tag-vegan"]')).not.toBeNull()
  })

  it('opens Details with the notice, the new lists and the other values kept, when the submission answers detailsChanged', async () => {
    const host = mountVariant()
    await openDetails(host)
    await press(host, '[data-testid="details-tag-tag-spicy"]')
    await press(host, '[data-testid="details-tag-tag-vegan"]')
    await nextStep(host)
    vi.mocked(post).mockResolvedValue({
      success: false,
      data: null,
      errorMessage: 'changed',
      status: 400,
      code: 'detailsChanged',
      body: { choices: NEW_CHOICES },
    })

    await press(host, '[data-testid="wizard-submit"]')

    expect(host.querySelector('[data-testid="details-step"]')).not.toBeNull()
    expect(host.querySelector('[data-testid="details-changed"]')).not.toBeNull()
    expect(host.querySelector('[data-testid="details-tag-tag-spicy"]')).toBeNull()
    expect(host.querySelector<HTMLInputElement>('[data-testid="details-tag-tag-vegan"]')?.checked).toBe(true)
    expect(host.querySelector('[data-testid="back-to-review"]')).not.toBeNull()
  })

  it('clears the notice once the member edits Details', async () => {
    const host = mountVariant()
    await openDetails(host)
    await nextStep(host)
    vi.mocked(post).mockResolvedValue({
      success: false,
      data: null,
      errorMessage: 'changed',
      status: 400,
      code: 'detailsChanged',
      body: { choices: NEW_CHOICES },
    })
    await press(host, '[data-testid="wizard-submit"]')

    await press(host, '[data-testid="details-tag-tag-vegan"]')

    expect(host.querySelector('[data-testid="details-changed"]')).toBeNull()
  })
})
