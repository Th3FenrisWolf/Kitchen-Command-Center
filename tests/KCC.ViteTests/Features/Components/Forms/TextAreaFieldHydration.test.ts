// @vitest-environment happy-dom
import { createSSRApp, defineComponent, h, nextTick, ref } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it, vi } from 'vitest'
import TextAreaField from '~/Components/Forms/TextAreaField.vue'

async function hydrate(serverValue: string, laterValue = serverValue) {
  const model = ref(serverValue)
  const Host = defineComponent({
    render: () => h(TextAreaField, { modelValue: model.value, 'onUpdate:modelValue': (v: string) => (model.value = v) }),
  })
  const root = document.createElement('div')
  root.innerHTML = await renderToString(createSSRApp(Host))
  document.body.append(root)
  const field = root.querySelector('textarea')!
  model.value = laterValue
  return { model, root, field, mount: () => createSSRApp(Host).mount(root) }
}

describe('TextAreaField hydrating after its server HTML was typed into', () => {
  it('keeps the typed text and the same node, and hands the text to the model', async () => {
    const { model, field, mount } = await hydrate('')
    field.value = 'typed before the chunk arrived'
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})

    mount()
    await nextTick()

    const warnings = warn.mock.calls.map((call) => String(call[0]))
    warn.mockRestore()
    expect(warnings.filter((message) => message.includes('mismatch'))).toEqual([])

    expect(document.body.querySelector('textarea')).toBe(field)
    expect(field.value).toBe('typed before the chunk arrived')
    expect(model.value).toBe('typed before the chunk arrived')
  })

  it('shows what the model holds when nothing was typed', async () => {
    const { field, mount } = await hydrate('', 'a review loaded meanwhile')

    mount()
    await nextTick()

    expect(field.value).toBe('a review loaded meanwhile')
  })

  it('keeps writing later model changes into the field', async () => {
    const { model, field, mount } = await hydrate('first')

    mount()
    model.value = 'second'
    await nextTick()

    expect(field.value).toBe('second')
  })
})
