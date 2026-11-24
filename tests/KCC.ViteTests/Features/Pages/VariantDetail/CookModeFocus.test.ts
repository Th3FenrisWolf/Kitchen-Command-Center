// @vitest-environment happy-dom
import { afterEach, describe, expect, it } from 'vitest'
import { createApp, h, nextTick, ref } from 'vue'
import { ResourceString } from '~/Components/ResourceStrings'
import CookMode from '~/Pages/VariantDetail/CookMode.vue'
import '~/Utilities/StringExtensions'

const unmounts: (() => void)[] = []

async function openCookMode() {
  const host = document.createElement('div')
  document.body.append(host)
  const open = ref(false)
  const app = createApp({
    render: () =>
      h(CookMode, {
        open: open.value,
        instructions: [{ text: 'Boil the gnocchi.' }, { text: 'Brown the butter.' }],
        ingredients: [],
        servings: 2,
        resourceStrings: { 'VariantDetail.CookMode': 'Cook mode', 'VariantDetail.Close': 'Close' },
      }),
  })
  app.component('ResourceString', ResourceString).mount(host)
  unmounts.push(() => app.unmount())
  await nextTick()
  open.value = true
  for (let i = 0; i < 6; i++) await nextTick()
}

describe('cook mode focus on open', () => {
  afterEach(() => {
    unmounts.splice(0).forEach((unmount) => unmount())
    document.body.innerHTML = ''
  })

  it('moves focus to its close control, so the focus ring lands on a control and not around the viewport', async () => {
    await openCookMode()

    expect(document.activeElement).toBe(document.querySelector('[data-test="cook-close"]'))
  })

  it('keeps the kit focus ring on its panel', async () => {
    await openCookMode()

    expect(document.querySelector('[role="dialog"] [tabindex="-1"]')?.classList).not.toContain('outline-none')
  })
})
