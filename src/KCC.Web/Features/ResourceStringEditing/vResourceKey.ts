import type { Directive } from 'vue'

// Tag a Vue-rendered text element as an editable resource string.
// Adds data-resource-key + class so the editor TS module discovers it via the
// MutationObserver. No-op when the editor bundle isn't loaded — the marker
// attribute is harmless on production pages.

export const vResourceKey: Directive<HTMLElement, string | null | undefined> = {
  mounted(el, binding) {
    apply(el, binding.value)
  },
  updated(el, binding) {
    apply(el, binding.value)
  },
}

function apply(el: HTMLElement, key: string | null | undefined): void {
  if (!key) {
    el.removeAttribute('data-resource-key')
    el.classList.remove('kcc-rs-editable')
    return
  }
  el.setAttribute('data-resource-key', key)
  el.classList.add('kcc-rs-editable')
}
