import type { ObjectDirective } from 'vue'
import { attachInk, type InkBinding } from '~/Ink/inkDom'

const disposers = new WeakMap<HTMLElement, () => void>()
const fingerprint = (value: InkBinding) => JSON.stringify(value ?? null)

/**
 * `v-ink` draws the sketch outline (and optional hatch) around its element: `v-ink`, `v-ink="'button'"` or
 * `v-ink="{ kind: 'sheet', double: true }"`; `null` draws nothing. Client-only — SSR renders no outline and
 * it fades in after hydration (umbrella spec §13, "revisit later").
 */
export const vInk: ObjectDirective<HTMLElement, InkBinding> = {
  mounted(el, binding) {
    disposers.set(el, attachInk(el, binding.value))
  },
  updated(el, binding) {
    if (fingerprint(binding.value) === fingerprint(binding.oldValue)) return
    disposers.get(el)?.()
    disposers.set(el, attachInk(el, binding.value))
  },
  unmounted(el) {
    disposers.get(el)?.()
    disposers.delete(el)
  },
  getSSRProps: () => ({}),
}
