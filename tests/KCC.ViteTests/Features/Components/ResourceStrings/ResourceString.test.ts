import { describe, expect, it } from 'vitest'
import { defineComponent, h, provide } from 'vue'
import ResourceString from '~/Components/ResourceStrings/ResourceString.Component.vue'
import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'
import { renderSsr } from '../../../support/ssr'

const Host = defineComponent({
  props: { preview: Boolean, for: { type: String, default: 'SignIn' } },
  setup(props) {
    provide('isPreview', props.preview)
    provide(resourceStringsKey, { strings: { 'Login.SignIn': 'Sign in' }, prefix: 'Login' })
    return () => h(ResourceString, { for: props.for })
  },
})

describe('ResourceString', () => {
  it('renders the resolved value', async () => {
    expect(await renderSsr(Host)).toContain('Sign in')
  })

  it('falls back to the full key when the value is missing', async () => {
    expect(await renderSsr(Host, { for: 'Missing' })).toContain('Login.Missing')
  })

  it('adds no edit markers in preview', async () => {
    const html = await renderSsr(Host, { preview: true })

    expect(html).not.toContain('kcc-rs-editable')
    expect(html).not.toContain('data-resource-key')
  })
})
