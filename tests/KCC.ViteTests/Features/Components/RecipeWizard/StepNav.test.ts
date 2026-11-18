import { describe, expect, it } from 'vitest'
import StepNav from '~/Components/RecipeWizard/StepNav.vue'
import { classesOf, enclosingTag, renderInWizard } from '../../../support/recipeWizard'
import { openTag } from '../../../support/ssr'

const render = () => renderInWizard(StepNav, { canGoBack: true, canProceed: true, returnsToReview: true })

describe('StepNav', () => {
  it('wraps Back, Back to review and Next two rules apart, so every line and the text below sit on the rule', async () => {
    const html = await render()
    const forward = classesOf(enclosingTag(html, 'data-testid="back-to-review"'))
    const row = classesOf(enclosingTag(html, 'data-testid="back-to-review"', 1))

    expect(forward).toEqual(expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-0.5']))
    expect(row).toEqual(expect.arrayContaining(['kcc-row', 'justify-between']))
  })

  it('disables Back to review only when the step cannot return to it', async () => {
    const backToReview = async (canReview?: boolean) =>
      openTag(
        await renderInWizard(StepNav, { canGoBack: true, canProceed: true, returnsToReview: true, canReview }),
        'data-testid="back-to-review"',
      )

    expect(await backToReview(undefined)).not.toMatch(/\sdisabled/)
    expect(await backToReview(false)).toMatch(/\sdisabled/)
  })
})
