import { describe, expect, it } from 'vitest'
import TextAreaField from '~/Components/Forms/TextAreaField.vue'
import { renderSsr } from '../../../support/ssr'

describe('TextAreaField', () => {
  it('lands the id and the labelling attributes on the textarea, where a Field label can reach them', async () => {
    const html = await renderSsr(TextAreaField, { id: 'notes', 'aria-label': 'Notes', modelValue: '' })

    expect(html).toMatch(/<textarea [^>]*id="notes"/)
    expect(html).toMatch(/<textarea [^>]*aria-label="Notes"/)
    expect(html).not.toMatch(/<label [^>]*id="notes"/)
  })

  it('dresses the pill with the class and the height a caller passes', async () => {
    const html = await renderSsr(TextAreaField, { class: 'shrink grow', modelValue: '' })

    expect(html).toMatch(/<label class="kcc-field kcc-field--area relative w-full shrink grow"/)
  })

  it('opens three rules tall, so whatever follows the field starts on the ruling', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '' })

    expect(html).toContain('height:72px')
  })

  it('lets the resize grip inherit the body size', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '' })

    expect(html).toContain('fa-grip-lines')
    expect(html).not.toMatch(/fa-grip-lines[^"]*text-\[/)
  })
})
