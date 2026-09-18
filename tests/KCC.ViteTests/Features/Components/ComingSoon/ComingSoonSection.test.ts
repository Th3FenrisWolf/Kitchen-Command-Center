import { describe, expect, it } from 'vitest'
import ComingSoonSection from '~/Components/ComingSoon/ComingSoonSection.vue'
import { renderSsr } from '../../../support/renderSsr'

describe('ComingSoonSection', () => {
  it('keeps its editor hooks in the label slot and washes lavender on the default tear', async () => {
    const html = await renderSsr(ComingSoonSection, { textKey: 'RankingComingSoon' })

    expect(html).toContain('kcc-slip kcc-tear-3')
    expect(html).toContain('--c:var(--color-lavender)')
    expect(html.match(/kcc-label/g)).toHaveLength(1)
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-hourglass-half" aria-hidden="true">')
    // No resource-strings provider in this render: <ResourceString> falls back to its own resolved key.
    expect(html).toContain('Shared.ComingSoon')
  })

  it('takes a different tear so neighbours never share one', async () => {
    const html = await renderSsr(ComingSoonSection, { textKey: 'RankingComingSoon', tear: 5 })

    expect(html).toContain('kcc-slip kcc-tear-5')
  })
})
