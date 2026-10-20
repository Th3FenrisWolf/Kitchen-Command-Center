import { describe, expect, it } from 'vitest'
import TextAreaField from '~/Components/Forms/TextAreaField.vue'
import { draggedRules } from '~/Components/Forms/draggedRules'
import { renderSsr } from '../../../support/ssr'

describe('TextAreaField', () => {
  it('keeps a caller aria-invalid when the field has no error, and still marks an error invalid', async () => {
    const forwarded = await renderSsr(TextAreaField, { modelValue: '', 'aria-invalid': 'true' })
    const errored = await renderSsr(TextAreaField, { modelValue: '', 'aria-invalid': 'false', error: true })

    expect(forwarded).toMatch(/<textarea [^>]*aria-invalid="true"/)
    expect(errored).toMatch(/<textarea [^>]*aria-invalid="true"/)
  })

  it('lands the id and the labelling attributes on the textarea, where a Field label can reach them', async () => {
    const html = await renderSsr(TextAreaField, { id: 'notes', 'aria-label': 'Notes', modelValue: '' })

    expect(html).toMatch(/<textarea [^>]*id="notes"/)
    expect(html).toMatch(/<textarea [^>]*aria-label="Notes"/)
    expect(html).not.toMatch(/<label [^>]*id="notes"/)
  })

  it('dresses the field with the class and the height a caller passes', async () => {
    const html = await renderSsr(TextAreaField, { class: 'shrink grow', modelValue: '' })

    expect(html).toMatch(/<label class="kcc-field kcc-field--area relative w-full shrink grow"/)
  })

  it('opens three rules tall, so whatever follows the field starts on the ruling', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '' })

    expect(html).toContain('height:calc(var(--bl) * 3)')
    expect(html).not.toMatch(/height:\d+px/)
  })

  it('lets the resize grip inherit the body size', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '' })

    expect(html).toContain('fa-grip-lines')
    expect(html).not.toMatch(/fa-grip-lines[^"]*text-\[/)
  })

  it('draws the error outline and marks the textarea invalid when the field has an error', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '', error: true })

    expect(html).toMatch(/<label class="kcc-field kcc-field--area kcc-field--error relative w-full"/)
    expect(html).toMatch(/<textarea [^>]*aria-invalid="true"/)
  })

  it('draws no error outline and claims nothing invalid without an error', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '' })

    expect(html).not.toContain('kcc-field--error')
    expect(html).not.toContain('aria-invalid')
  })
})

describe('TextAreaField starting height', () => {
  it('opens at the rules a caller asks for', async () => {
    const html = await renderSsr(TextAreaField, { modelValue: '', rules: 4 })

    expect(html).toContain('height:calc(var(--bl) * 4)')
  })
})

describe('draggedRules', () => {
  it('moves a rule for each rule of pointer travel', () => {
    expect(draggedRules({ startRules: 3, floor: 3, deltaY: 48, rulePx: 24 })).toBe(5)
  })

  it('never drops below the starting height', () => {
    expect(draggedRules({ startRules: 4, floor: 4, deltaY: -240, rulePx: 24 })).toBe(4)
  })

  it('keeps two rules as the floor even when asked for less', () => {
    expect(draggedRules({ startRules: 2, floor: 1, deltaY: -240, rulePx: 24 })).toBe(2)
  })
})
