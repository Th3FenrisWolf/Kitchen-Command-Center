import { describe, expect, it } from 'vitest'
import StepNav from '~/Components/RecipeWizard/StepNav.vue'
import { classesOf, enclosingTag, renderInWizard } from '../../../support/recipeWizard'

const render = () => renderInWizard(StepNav, { canGoBack: true, canProceed: true, returnsToReview: true })

describe('StepNav', () => {
  it('wraps Back, Back to review and Next onto the next rule, never 12px under', async () => {
    const html = await render()
    const forward = classesOf(enclosingTag(html, 'data-testid="back-to-review"'))
    const row = classesOf(enclosingTag(html, 'data-testid="back-to-review"', 1))

    expect(forward).toEqual(expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-6']))
    expect(row).toEqual(expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-6']))
  })
})
