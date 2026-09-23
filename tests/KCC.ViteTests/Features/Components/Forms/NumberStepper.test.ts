import { describe, expect, it } from 'vitest'
import NumberStepper from '~/Components/Forms/NumberStepper.vue'
import { renderSsr } from '../../../support/renderSsr'

const render = (props: Record<string, unknown> = {}) => renderSsr(NumberStepper, { modelValue: 4, min: 1, ...props })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('NumberStepper names', () => {
  it('composes the button names from the label when a page gives none', async () => {
    const html = await render({ label: 'Makes' })

    expect(html).toContain('aria-label="Decrease Makes"')
    expect(html).toContain('aria-label="Increase Makes"')
    expect(html).toContain('aria-label="Makes"')
  })

  it('lets a page name the buttons in its own strings', async () => {
    const html = await render({ label: 'Makes', decreaseLabel: 'Fewer', increaseLabel: 'More' })

    expect(html).toContain('aria-label="Fewer"')
    expect(html).toContain('aria-label="More"')
    expect(html).not.toContain('Decrease Makes')
  })
})

describe('NumberStepper buttons', () => {
  it('are square ghost kit pills with the glyph as their only content', async () => {
    const html = await render({ label: 'Makes' })
    const decrease = tagWith(html, 'aria-label="Decrease Makes"')

    expect(decrease).toContain('kcc-btn kcc-btn--ghost kcc-btn--icon')
    expect(decrease).not.toContain('w-9')
    expect(html).toContain('<i class="fa-duotone fa-minus" aria-hidden="true"></i>')
    expect(html).toContain('<i class="fa-duotone fa-plus" aria-hidden="true"></i>')
  })

  it('disables the minus at the minimum', async () => {
    const atMin = tagWith(await render({ modelValue: 1, label: 'Makes' }), 'aria-label="Decrease Makes"')

    expect(atMin).toContain('disabled')
    expect(tagWith(await render({ label: 'Makes' }), 'aria-label="Decrease Makes"')).not.toContain('disabled')
  })
})
