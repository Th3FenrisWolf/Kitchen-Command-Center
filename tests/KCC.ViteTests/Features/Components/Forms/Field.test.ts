import { h } from 'vue'
import { describe, expect, it } from 'vitest'
import Field from '~/Components/Forms/Field.vue'
import { renderSsr } from '../../../support/renderSsr'

const baseProps = { label: 'Email', controlId: 'email' }

// The slotted control (InputField, TextAreaField, …) reads `error` off the scoped slot to add
// `kcc-field--error` to its own pill; a plain input stands in for it here.
const control = (slotProps?: { error?: boolean }) =>
  h('input', { id: 'email', class: { 'kcc-field--error': !!slotProps?.error } })

describe('Field', () => {
  it('renders the label as a kick and no well when clean', async () => {
    const html = await renderSsr(Field, baseProps, { default: control })
    expect(html).toContain('class="kcc-lbl"')
    expect(html).not.toContain('kcc-well')
    expect(html).not.toContain('kcc-field--error')
  })

  it('shows the hint in ink when there is no error', async () => {
    const html = await renderSsr(Field, { ...baseProps, hint: 'We will not share this' }, { default: control })
    expect(html).toContain('class="kcc-kick text-ink"')
    expect(html).toContain('We will not share this')
    expect(html).not.toContain('kcc-well')
  })

  it('adds kcc-field--error to the slotted control and renders a danger well, never red text', async () => {
    const html = await renderSsr(Field, { ...baseProps, error: 'Enter an email address' }, { default: control })
    expect(html).toContain('kcc-field--error')
    expect(html).toContain('class="kcc-well kcc-well--danger kcc-kick"')
    expect(html).toContain('role="alert"')
    expect(html).toContain('Enter an email address')
    expect(html).not.toContain('text-danger-ink')
    expect(html).not.toContain('text-red')
  })

  it('prefers the error over the hint when both are set', async () => {
    const html = await renderSsr(Field, { ...baseProps, hint: 'ignored', error: 'Required' }, { default: control })
    expect(html).toContain('kcc-well--danger')
    expect(html).not.toContain('ignored')
  })
})
