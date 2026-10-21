import { describe, expect, it } from 'vitest'
import NumberStepper from '~/Components/Forms/NumberStepper.vue'
import { renderSsr, tagWith } from '../../../support/ssr'

const render = (props: Record<string, unknown> = {}) => renderSsr(NumberStepper, { modelValue: 4, min: 1, ...props })

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

describe('NumberStepper value name', () => {
  it('names the value from its label and unit when a page gives no value label', async () => {
    expect(await render({ label: 'Prep', unit: 'min' })).toContain('aria-label="Prep in min"')
  })

  it('calls the value "Value" when it has no label', async () => {
    expect(await render()).toContain('aria-label="Value"')
  })

  it('lets a page name the value in its own strings, in place of "{label} in {unit}"', async () => {
    const html = await render({ label: 'Prep', unit: 'min', valueLabel: 'Prep time in minutes' })

    expect(html).toContain('aria-label="Prep time in minutes"')
    expect(html).not.toContain('aria-label="Prep in min"')
  })

  it('lets a page name an unlabelled value in place of "Value"', async () => {
    const html = await render({ valueLabel: 'Amount' })

    expect(html).toContain('aria-label="Amount"')
    expect(html).not.toContain('aria-label="Value"')
  })
})

describe('NumberStepper field', () => {
  it('is a stepper field, which draws no X: a stepper is a control, not a line to write on', async () => {
    const field = tagWith(await render({ label: 'Makes' }), 'kcc-field')

    expect(field).toContain('kcc-field kcc-field--stepper')
  })

  it('starts its unit on the number’s line, now the number fills the field’s height, so the unit stays on the rule', async () => {
    const row = tagWith(await render({ label: 'Makes', unit: 'min' }), 'justify-center')

    expect(row).toContain('items-start')
    expect(row).not.toContain('items-center')
  })
})

describe('NumberStepper buttons', () => {
  it('start on the field’s line, so the kit centres them on its rule', async () => {
    const html = await render({ label: 'Makes' })
    const row = html.match(/^(?:<!--[\s\S]*?-->)*(<div[^>]*>)/)?.[1] ?? ''

    expect(row).toContain('items-start')
    expect(row).not.toContain('items-center')
  })

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
