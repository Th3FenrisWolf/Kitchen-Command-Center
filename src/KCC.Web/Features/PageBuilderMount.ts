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

// Vue component tag names that may appear as raw HTML tags in Kentico's
// widget injections. When a marker's subtree contains any of these, we know
// it still needs Vue rendering (either a fresh Kentico injection or Kentico
// replaced a previously-rendered widget with source cshtml output). If none
// are present, the marker has already been rendered by the main Vue app
// (via SSR + hydration) and we should leave it alone.
// Derived from the global component registry so new components are picked
// up automatically.
const RAW_VUE_TAGS = registeredTagNames

// CSS selector union for needsRendering's O(1) `querySelector` check.
// `:not(*)` matches nothing — a safe fallback when no components are registered.
const RAW_VUE_TAG_SELECTOR = RAW_VUE_TAGS.size > 0 ? [...RAW_VUE_TAGS].join(',') : ':not(*)'

// Track Vue apps we created, keyed by their rendered root element so we can
// tear them down when Kentico removes the widget.
const mountedApps = new WeakMap<Element, App>()

function mountMarker(marker: Element): void {
  // If we already own this element, we're being asked to re-mount because
  // Kentico replaced its content with raw Vue tags. Unmount the old app first
  // so its teardown runs before we compile the new template.
  const existing = mountedApps.get(marker)
  if (existing) {
    existing.unmount()
    mountedApps.delete(marker)
  }

  // Use outerHTML so the marker tag itself is part of the template. This lets
  // Vue resolve component tag markers (like <Card>) to their component class.
  // Invariant: the marker lives inside Kentico's ktc-widget-body-wrapper,
  // which does not inject Kentico chrome (drag handles, edit UI) into the
  // marker's descendants. If that invariant ever breaks, Vue will try to
  // compile unknown Kentico elements and warn.
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

// Evaluate a marker and take the appropriate action. Idempotent:
//   - Needs rendering + not mounted: mount.
//   - Needs rendering + already mounted: unmount + re-mount (Kentico reset).
//   - No raw tags + mounted by us: leave alone (it's our rendered output).
//   - No raw tags + not mounted: SSR-owned; leave alone.
function evaluateMarker(marker: Element): void {
  const isOurs = mountedApps.has(marker)
  const hasRawTags = needsRendering(marker)

  if (hasRawTags) {
    mountMarker(marker)
  } else if (!isOurs) {
    // SSR-rendered marker, not ours. Nothing to do.
  }
}

function processAddedNode(node: Node): void {
  if (node.nodeType !== Node.ELEMENT_NODE) return
  for (const marker of findMarkers(node as Element)) {
    evaluateMarker(marker)
  }
}

// Called for every element in MutationRecord.removedNodes, including the
// subtree-wide removal pattern (parent.innerHTML = '...'). findMarkers walks
// the removed node's subtree, so a Vue app whose root element was detached
// is always unmounted — no WeakMap-GC leak path.
function processRemovedNode(node: Node): void {
  if (node.nodeType !== Node.ELEMENT_NODE) return
  for (const marker of findMarkers(node as Element)) {
    unmountMarker(marker)
  }
}

function processMutationTarget(target: Node): void {
  // Kentico sometimes replaces a widget's content via `.innerHTML = ...` on a
  // parent element. That reports as a childList mutation on the parent and no
  // top-level element in addedNodes matches our marker selector. Re-scan the
  // target itself so already-claimed markers get re-evaluated when their
  // contents change.
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
    // Edit mode: attach to Kentico's declared editable zones so the observer
    // scope matches where Kentico actually injects widgets.
    areas.forEach(observeArea)
    return
  }

  // ReadOnly / preview mode: Kentico doesn't mark editable areas, but the
  // page may still contain raw Vue tags from cshtml that didn't reach SSR
  // (e.g., inline widget preview). Observe the whole body so any such tags
  // get compiled.
  observeArea(document.body)
}

// This script is loaded as type="module" (see Layout.cshtml), which defers
// execution until after the document is parsed, so readyState is at least
// 'interactive' when we reach this line. The 'loading' branch is defensive
// only — kept in case the script ever gets loaded synchronously (<script>
// without type="module") by future changes.
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize)
} else {
  initialize()
}
