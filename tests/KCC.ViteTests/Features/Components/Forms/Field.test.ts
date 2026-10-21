import { h } from 'vue'
import { describe, expect, it } from 'vitest'
import Field from '~/Components/Forms/Field.vue'
import { renderSsr } from '../../../support/ssr'
import { classesOf, enclosingTag } from '../../../support/recipeWizard'

const baseProps = { label: 'Email', controlId: 'email' }

// A caller hands the scoped slot's `error` to its control, and InputField, TextAreaField and SelectField add
// `kcc-field--error` to their line from it; a plain input stands in for them here.
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

  it('keys the hint and error ids off the control id and hands the live one to the slot', async () => {
    // A real control binds the slot's `describedby` straight to aria-describedby; the ids have to line up
    // or the hint and the error are invisible to a screen reader.
    const described = (slotProps?: { describedby?: string }) =>
      h('input', { id: 'email', 'aria-describedby': slotProps?.describedby })

    const hinted = await renderSsr(Field, { ...baseProps, hint: 'We will not share this' }, { default: described })
    expect(hinted).toContain('id="email-hint"')
    expect(hinted).toContain('aria-describedby="email-hint"')

    const errored = await renderSsr(Field, { ...baseProps, error: 'Enter an email address' }, { default: described })
    expect(errored).toContain('id="email-error"')
    expect(errored).toContain('aria-describedby="email-error"')
  })

  it('takes rich label content from the slot so a resource string keeps its editor hooks', async () => {
    const html = await renderSsr(
      Field,
      { controlId: 'email', required: true },
      { default: control, label: () => h('span', { 'data-resource-key': 'Account.Email' }, 'Email') },
    )

    expect(html).toContain('<label for="email" class="kcc-lbl">')
    expect(html).toContain('<span data-resource-key="Account.Email">Email</span>')
    expect(html).toContain('<span aria-hidden="true"> *</span>')
  })

  it('takes rich hint content from the slot, still describedby-ing the same id', async () => {
    const described = (slotProps?: { describedby?: string }) =>
      h('input', { id: 'email', 'aria-describedby': slotProps?.describedby })

    const html = await renderSsr(Field, baseProps, {
      default: described,
      hint: () => h('span', { 'data-resource-key': 'Account.EmailComingSoon' }, 'Coming soon'),
    })

    expect(html).toContain('aria-describedby="email-hint"')
    expect(html).toContain('<p id="email-hint" class="kcc-kick text-ink">')
    expect(html).toContain('<span data-resource-key="Account.EmailComingSoon">Coming soon</span>')
    expect(html).not.toContain('kcc-well')
  })

  it('stacks label, control and message with no gap, the control at the top of a box two rules tall, on a line', async () => {
    const html = await renderSsr(Field, { ...baseProps, hint: 'We will not share this' }, { default: control })

    expect(classesOf(html.match(/^<div[^>]*>/)?.[0] ?? '')).toEqual(['flex', 'flex-col'])
    expect(classesOf(enclosingTag(html, 'id="email"'))).toEqual(['flex', 'min-h-rule-2', 'flex-col'])
  })

  it('sets what follows the control on the next rule, outside the control box with no gap', async () => {
    const html = await renderSsr(
      Field,
      { ...baseProps, hint: 'We will not share this' },
      { default: control, after: () => h('span', { 'data-testid': 'value-mark' }, 'Read from the photo') },
    )
    const root = html.match(/^<div[^>]*>/)?.[0] ?? ''
    const box = enclosingTag(html, 'id="email"')
    const boxEnd = html.indexOf('</div>', html.indexOf(box))

    expect(enclosingTag(html, 'data-testid="value-mark"')).toBe(root)
    expect(enclosingTag(html, 'id="email-hint"')).toBe(root)
    expect(html.indexOf('data-testid="value-mark"')).toBeGreaterThan(boxEnd)
    expect(html.indexOf('id="email-hint"')).toBeGreaterThan(html.indexOf('data-testid="value-mark"'))
    expect([...classesOf(root), ...classesOf(box)].filter((name) => /^(gap|space)-/.test(name))).toEqual([])
  })

  it('prefers the error over the hint when both are set', async () => {
    const html = await renderSsr(Field, { ...baseProps, hint: 'ignored', error: 'Required' }, { default: control })
    expect(html).toContain('kcc-well--danger')
    expect(html).not.toContain('ignored')
  })
})
