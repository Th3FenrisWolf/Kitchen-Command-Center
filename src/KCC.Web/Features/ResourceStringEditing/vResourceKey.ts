import type { Directive } from 'vue'

// Tag a Vue-rendered text element as an editable resource string.
// Adds data-resource-key + class so the editor TS module discovers it on its
// initial DOM scan. getSSRProps emits the marker during server rendering so
// the attribute is in the HTML before any JS runs — matching @Html.ResourceString.
// The mounted/updated hooks keep the marker in sync when binding values change
// client-side (e.g. Kentico Page Builder edit-mode widget mounts).

export const vResourceKey: Directive<HTMLElement, string | null | undefined> = {
  getSSRProps(binding) {
    if (!binding.value) return {}
    return {
      'data-resource-key': binding.value,
      class: 'kcc-rs-editable',
    }
  },
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
