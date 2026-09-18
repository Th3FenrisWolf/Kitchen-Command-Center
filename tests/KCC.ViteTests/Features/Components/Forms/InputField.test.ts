import { describe, expect, it } from 'vitest'
import InputField from '~/Components/Forms/InputField.vue'
import { renderSsr } from '../../../support/renderSsr'

// The component's root is the pill (`<label>`), not the control, so the attribute split is the contract:
// class/style dress the pill, everything else belongs to the <input>.
const labelTag = (html: string) => html.match(/<label[^>]*>/)![0]
const inputTag = (html: string) => html.match(/<input[^>]*>/)![0]

describe('InputField', () => {
  it('is a full-width pill wrapped around the input', async () => {
    const html = await renderSsr(InputField, { modelValue: '' })
    expect(html).toContain('<label class="kcc-field kcc-field--noicon w-full"')
    expect(html).toContain('<input')
  })

  it('lands a caller class on the pill beside the kit classes, never on the input', async () => {
    const html = await renderSsr(InputField, { modelValue: '', class: 'basis-1/3' })
    expect(labelTag(html)).toContain('class="kcc-field kcc-field--noicon w-full basis-1/3"')
    expect(inputTag(html)).not.toContain('basis-1/3')
  })

  it('lands every other attribute on the input, never on the pill', async () => {
    const html = await renderSsr(InputField, {
      modelValue: '',
      id: 'email',
      placeholder: 'you@example.com',
      'aria-describedby': 'email-hint',
    })
    const input = inputTag(html)
    const label = labelTag(html)
    for (const attr of ['id="email"', 'placeholder="you@example.com"', 'aria-describedby="email-hint"']) {
      expect(input).toContain(attr)
      expect(label).not.toContain(attr)
    }
  })

  it('drops --noicon and prints the leading glyph when an icon is given', async () => {
    const html = await renderSsr(InputField, { modelValue: '', icon: 'fa-duotone fa-magnifying-glass' })
    expect(html).toContain('<label class="kcc-field w-full"')
    expect(html).not.toContain('kcc-field--noicon')
    expect(html).toContain('<i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>')
    // The icon is the pill's first grid column, so it has to render before the control.
    expect(html.indexOf('<i ')).toBeLessThan(html.indexOf('<input'))
  })
})
