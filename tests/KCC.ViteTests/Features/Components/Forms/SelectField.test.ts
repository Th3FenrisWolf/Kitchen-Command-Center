import { h } from 'vue'
import { describe, expect, it } from 'vitest'
import SelectField from '~/Components/Forms/SelectField.vue'
import { renderSsr } from '../../../support/ssr'

const options = () => [h('option', { value: 'a' }, 'Apple'), h('option', { value: 'b' }, 'Bread')]

const lineTag = (html: string) => html.match(/<span[^>]*>/)![0]
const selectTag = (html: string) => html.match(/<select[^>]*>/)![0]

describe('SelectField', () => {
  it('keeps a caller aria-invalid when the field has no error, and still marks an error invalid', async () => {
    const forwarded = await renderSsr(SelectField, { 'aria-invalid': 'true' }, { default: options })
    const errored = await renderSsr(SelectField, { 'aria-invalid': 'false', error: true }, { default: options })

    expect(selectTag(forwarded)).toContain('aria-invalid="true"')
    expect(selectTag(errored)).toContain('aria-invalid="true"')
  })

  it('is a full-width line wrapped around the select and its options', async () => {
    const html = await renderSsr(SelectField, { id: 'pick' }, { default: options })

    expect(lineTag(html)).toContain('class="kcc-field w-full"')
    expect(selectTag(html)).toContain('id="pick"')
    expect(html).toContain('<option value="b">Bread</option>')
  })

  it('lands a caller class on the line and every other attribute on the select', async () => {
    const html = await renderSsr(
      SelectField,
      { id: 'pick', class: 'basis-60', 'aria-describedby': 'pick-hint' },
      { default: options },
    )

    expect(lineTag(html)).toContain('class="kcc-field w-full basis-60"')
    expect(lineTag(html)).not.toContain('pick-hint')
    expect(selectTag(html)).toContain('aria-describedby="pick-hint"')
    expect(selectTag(html)).not.toContain('basis-60')
  })

  it('draws the error line and marks the select invalid when the field has an error', async () => {
    const html = await renderSsr(SelectField, { id: 'pick', error: true }, { default: options })

    expect(lineTag(html)).toContain('class="kcc-field kcc-field--error w-full"')
    expect(selectTag(html)).toContain('aria-invalid="true"')
  })

  it('draws no error line and claims nothing invalid without an error', async () => {
    const html = await renderSsr(SelectField, { id: 'pick' }, { default: options })

    expect(html).not.toContain('kcc-field--error')
    expect(html).not.toContain('aria-invalid')
  })
})
