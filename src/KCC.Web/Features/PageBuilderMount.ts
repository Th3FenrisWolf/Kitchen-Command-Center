import { createApp, type App } from 'vue'
import { registerGlobalComponents, registeredTagNames } from '~/GlobalComponents'

// Edit-mode adapter for Kentico Page Builder.
//
// In edit mode, Kentico dynamically injects raw widget HTML (with Vue tags like
// <Card>, <Stacker>) into editable-area zones via AJAX. The main Vue app has
// already hydrated by then, so those injections land outside its reactive tree.
//
// Widget cshtml marks its Vue-rendered region with a data-vue-mount attribute.
// The marker can be either:
//   - An existing native element (e.g., CardGridWidget's <div class="grid" data-vue-mount>)
//   - A Vue component tag itself (e.g., CardWidget's <Card data-vue-mount>)
//
// For each new marker Kentico injects, this adapter:
//   1. Takes the marker's outerHTML as the Vue template
//   2. Mounts a fresh Vue app on a detached container, compiling the template
//   3. Replaces the marker in the DOM with Vue's rendered output
//
// Because replacement preserves the marker's position within Kentico's
// ktc-widget-body-wrapper, Kentico's edit chrome (drag handles, edit buttons)
// is untouched. No extra DOM level is introduced.
//
// This script is conditionally loaded from Layout.cshtml only when
// Context.Kentico().PageBuilder().EditMode is true; it never runs on live pages.

const MOUNT_SELECTOR = '[data-vue-mount]'

const RAW_VUE_TAGS = registeredTagNames

// CSS selector union for needsRendering's O(1) `querySelector` check.
// `:not(*)` matches nothing — a safe fallback when no components are registered.
const RAW_VUE_TAG_SELECTOR = RAW_VUE_TAGS.size > 0 ? [...RAW_VUE_TAGS].join(',') : ':not(*)'

// Track Vue apps we created, keyed by their rendered root element so we can
// tear them down when Kentico removes the widget.
const mountedApps = new WeakMap<Element, App>()

function mountMarker(marker: Element): void {
  const existing = mountedApps.get(marker)
  if (existing) {
    existing.unmount()
    mountedApps.delete(marker)
  }

  const template = marker.outerHTML
  if (!template.trim()) return

  const container = document.createElement('div')

  const app = createApp({ template })
  registerGlobalComponents(app)
  app.mount(container)

  const parent = marker.parentNode
  if (!parent) {
    app.unmount()
    return
  }

  // Move Vue's rendered output into the marker's position, then remove marker.
  const rendered = Array.from(container.childNodes)
  rendered.forEach((child) => parent.insertBefore(child, marker))
  parent.removeChild(marker)

  // Vue forwards the data-vue-mount attribute (not a declared prop) to the
  // component's root element, so the rendered output keeps the marker attr.
  // Track the app by that element so we can unmount / re-mount it when
  // Kentico mutates the widget.
  const trackEl = rendered.find(
    (node): node is Element => node.nodeType === Node.ELEMENT_NODE && (node as Element).hasAttribute('data-vue-mount'),
  )
  if (trackEl) {
    mountedApps.set(trackEl, app)
  }
}

function unmountMarker(marker: Element): void {
  const app = mountedApps.get(marker)
  if (!app) return

  app.unmount()
  mountedApps.delete(marker)
}

function findMarkers(root: Element): Element[] {
  const found: Element[] = []
  if (root.matches(MOUNT_SELECTOR)) found.push(root)

  found.push(...root.querySelectorAll(MOUNT_SELECTOR))

  return found
}

function needsRendering(marker: Element): boolean {
  // Marker is itself a Vue tag (e.g. <Card data-vue-mount>).
  if (RAW_VUE_TAGS.has(marker.tagName.toLowerCase())) return true

  // Marker contains raw Vue tags anywhere in its subtree (e.g. Kentico reset
  // a CardGridWidget's innerHTML to source cshtml).
  return marker.querySelector(RAW_VUE_TAG_SELECTOR) !== null
}

function evaluateMarker(marker: Element): void {
  const hasRawTags = needsRendering(marker)

  if (hasRawTags) mountMarker(marker)
}

function processAddedNode(node: Node): void {
  if (node.nodeType !== Node.ELEMENT_NODE) return

  for (const marker of findMarkers(node as Element)) {
    evaluateMarker(marker)
  }
}

function processRemovedNode(node: Node): void {
  if (node.nodeType !== Node.ELEMENT_NODE) return

  for (const marker of findMarkers(node as Element)) {
    unmountMarker(marker)
  }
}

// Kentico sometimes replaces a widget's content via `.innerHTML = ...` on a
// parent element. That reports as a childList mutation on the parent and no
// top-level element in addedNodes matches our marker selector. Re-scan the
// target itself so already-claimed markers get re-evaluated when their
// contents change.
function processMutationTarget(target: Node): void {
  if (target.nodeType !== Node.ELEMENT_NODE) return

  for (const marker of findMarkers(target as Element)) {
    evaluateMarker(marker)
  }
}

function observeArea(area: Element): void {
  // Initial scan: evaluate every marker in the area.
  findMarkers(area).forEach(evaluateMarker)

  const observer = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      mutation.addedNodes.forEach(processAddedNode)
      mutation.removedNodes.forEach(processRemovedNode)

      if (mutation.type === 'childList' && mutation.target) {
        processMutationTarget(mutation.target)
      }
    }
  })

  observer.observe(area, { childList: true, subtree: true })
}

function initialize(): void {
  const areas = document.querySelectorAll<HTMLElement>('[data-kentico-editable-area-id]')

  if (areas.length > 0) {
    areas.forEach(observeArea)
    return
  }

  // ReadOnly / preview mode: fallback to observe entire body
  observeArea(document.body)
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize)
} else {
  initialize()
}
