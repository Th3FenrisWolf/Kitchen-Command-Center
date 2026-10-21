import { describe, expect, it } from 'vitest'
import InputField from '~/Components/Forms/InputField.vue'
import { renderSsr } from '../../../support/ssr'

// The component's root is the line (`<label>`), not the control, so the attribute split is the contract:
// class/style dress the line, everything else belongs to the <input>.
const labelTag = (html: string) => html.match(/<label[^>]*>/)![0]
const inputTag = (html: string) => html.match(/<input[^>]*>/)![0]

describe('InputField', () => {
  it('keeps a caller aria-invalid when the field has no error, and still marks an error invalid', async () => {
    const forwarded = await renderSsr(InputField, { modelValue: '', 'aria-invalid': 'true' })
    const errored = await renderSsr(InputField, { modelValue: '', 'aria-invalid': 'false', error: true })

    expect(inputTag(forwarded)).toContain('aria-invalid="true"')
    expect(inputTag(errored)).toContain('aria-invalid="true"')
  })

  it('is a full-width line wrapped around the input, with no icon', async () => {
    const html = await renderSsr(InputField, { modelValue: '' })
    expect(html).toContain('<label class="kcc-field w-full"')
    expect(html).toContain('<input')
    expect(html).not.toMatch(/<i[\s>]/)
  })

  it('lands a caller class on the line beside the kit classes, never on the input', async () => {
    const html = await renderSsr(InputField, { modelValue: '', class: 'basis-1/3' })
    expect(labelTag(html)).toContain('class="kcc-field w-full basis-1/3"')
    expect(inputTag(html)).not.toContain('basis-1/3')
  })

  it('lands an input class on the input, never on the line', async () => {
    const html = await renderSsr(InputField, { modelValue: '', inputClass: 'kcc-num' })
    expect(inputTag(html)).toMatch(/\sclass="kcc-num"/)
    expect(inputTag(html)).not.toMatch(/inputclass/i)
    expect(labelTag(html)).not.toContain('kcc-num')
  })

  it('lands every other attribute on the input, never on the line', async () => {
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

  it('takes no icon: the X before the line is the field’s one leading mark', async () => {
    expect(Object.keys(InputField.props ?? {})).not.toContain('icon')
    const html = await renderSsr(InputField, { modelValue: '' })
    expect(html).not.toContain('kcc-field--noicon')
    expect(html).not.toMatch(/<i[\s>]/)
  })

  it('draws the error line and marks the input invalid when the field has an error', async () => {
    const html = await renderSsr(InputField, { modelValue: '', error: true, class: 'basis-1/3' })
    expect(labelTag(html)).toContain('class="kcc-field kcc-field--error w-full basis-1/3"')
    expect(inputTag(html)).toContain('aria-invalid="true"')
    expect(inputTag(html)).not.toContain('error')
  })

  it('draws no error line and claims nothing invalid without an error', async () => {
    const html = await renderSsr(InputField, { modelValue: '' })
    expect(html).not.toContain('kcc-field--error')
    expect(html).not.toContain('aria-invalid')
  })
})
