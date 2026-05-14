// Inline resource string editor — loaded in page builder edit and preview modes.

import './ResourceStringEditor.css'

interface ResourceStringEditorContext {
  currentLanguage: string
  availableLanguages: { code: string; name: string }[]
  isPreviewMode: boolean
}

function readContext(): ResourceStringEditorContext {
  const el = document.getElementById('kcc-rs-editor-context')
  if (!el?.textContent) {
    return { currentLanguage: '', availableLanguages: [], isPreviewMode: false }
  }
  try {
    const parsed = JSON.parse(el.textContent)
    return {
      currentLanguage: typeof parsed?.currentLanguage === 'string' ? parsed.currentLanguage : '',
      availableLanguages: Array.isArray(parsed?.availableLanguages) ? parsed.availableLanguages : [],
      isPreviewMode: parsed?.isPreviewMode === true,
    }
  } catch {
    return { currentLanguage: '', availableLanguages: [], isPreviewMode: false }
  }
}

const ctx = readContext()

type PreviewMode = 'off' | 'view' | 'edit'
const MODE_CYCLE: PreviewMode[] = ['off', 'view', 'edit']
const MODE_STORAGE_KEY = 'kcc-rs-preview-mode'

let currentMode: PreviewMode = 'off'

const MODE_LABELS: Record<PreviewMode, string> = {
  off: 'Resource strings hidden',
  view: 'Resource strings visible',
  edit: 'Resource strings editable',
}

const SVG_NS = 'http://www.w3.org/2000/svg'
const MODE_PATHS: Record<PreviewMode, string[]> = {
  off: [
    'M12 7c2.76 0 5 2.24 5 5 0 .65-.13 1.26-.36 1.83l2.92 2.92c1.51-1.26 2.7-2.89 3.43-4.75-1.73-4.39-6-7.5-11-7.5-1.4 0-2.74.25-3.98.7l2.16 2.16C10.74 7.13 11.35 7 12 7zM2 4.27l2.28 2.28.46.46C3.08 8.3 1.78 10.02 1 12c1.73 4.39 6 7.5 11 7.5 1.55 0 3.03-.3 4.38-.84l.42.42L19.73 22 21 20.73 3.27 3 2 4.27zM7.53 9.8l1.55 1.55c-.05.21-.08.43-.08.65 0 1.66 1.34 3 3 3 .22 0 .44-.03.65-.08l1.55 1.55c-.67.33-1.41.53-2.2.53-2.76 0-5-2.24-5-5 0-.79.2-1.53.53-2.2zm4.31-.78l3.15 3.15.02-.16c0-1.66-1.34-3-3-3l-.17.01z',
  ],
  view: [
    'M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z',
  ],
  edit: [
    'M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z',
  ],
}

function createModeIcon(mode: PreviewMode): SVGSVGElement {
  const svg = document.createElementNS(SVG_NS, 'svg')
  svg.setAttribute('viewBox', '0 0 24 24')
  svg.setAttribute('width', '20')
  svg.setAttribute('height', '20')
  svg.setAttribute('fill', 'currentColor')
  svg.setAttribute('aria-hidden', 'true')
  for (const d of MODE_PATHS[mode]) {
    const path = document.createElementNS(SVG_NS, 'path')
    path.setAttribute('d', d)
    svg.appendChild(path)
  }
  return svg
}

function setPreviewMode(mode: PreviewMode): void {
  currentMode = mode
  document.body.classList.remove('kcc-rs-mode-view', 'kcc-rs-mode-edit')
  if (mode === 'view') document.body.classList.add('kcc-rs-mode-view')
  if (mode === 'edit') document.body.classList.add('kcc-rs-mode-edit')

  hideAllPencils()
  if (mode !== 'edit') closePopover()

  try {
    sessionStorage.setItem(MODE_STORAGE_KEY, mode)
  } catch {
    /* quota */
  }
}

const MARKER_SELECTOR = '[data-resource-key]'
const PENCIL_CLASS = 'kcc-rs-pencil'

function hideAllPencils(): void {
  document.querySelectorAll(`.${PENCIL_CLASS}`).forEach((el) => {
    ;(el as HTMLElement).style.display = 'none'
  })
}

function isEditActive(): boolean {
  return !ctx.isPreviewMode || currentMode === 'edit'
}

function updateFabIcon(fab: HTMLElement): void {
  fab.replaceChildren(createModeIcon(currentMode))
  fab.setAttribute('aria-label', MODE_LABELS[currentMode])
}

function buildFab(): void {
  const fab = document.createElement('button')
  fab.type = 'button'
  fab.className = 'kcc-rs-mode-fab'
  updateFabIcon(fab)
  document.body.appendChild(fab)

  fab.addEventListener('click', () => {
    const idx = MODE_CYCLE.indexOf(currentMode)
    const next = MODE_CYCLE[(idx + 1) % MODE_CYCLE.length] ?? 'off'
    setPreviewMode(next)
    updateFabIcon(fab)
  })
}

interface MarkerState {
  pencil: HTMLElement | null
}

const markers = new WeakMap<Element, MarkerState>()

function ensurePencil(target: Element): HTMLElement {
  const existing = markers.get(target)?.pencil
  if (existing) return existing

  const pencil = document.createElement('button')
  pencil.type = 'button'
  pencil.className = PENCIL_CLASS
  pencil.setAttribute('aria-label', 'Edit resource string')
  pencil.textContent = '✎' // pencil glyph (✎)

  document.body.appendChild(pencil)
  markers.set(target, { pencil })

  pencil.addEventListener('click', (e) => {
    e.preventDefault()
    e.stopPropagation()
    if (!isEditActive()) return
    const key = target.getAttribute('data-resource-key')
    if (!key) return
    openPopover(target, key)
  })

  return pencil
}

function positionPencil(target: Element, pencil: HTMLElement): void {
  const rect = target.getBoundingClientRect()
  pencil.style.position = 'absolute'
  pencil.style.top = `${rect.top + window.scrollY - 4}px`
  pencil.style.left = `${rect.right + window.scrollX + 4}px`
}

function attachHover(target: Element): void {
  if (markers.has(target)) return
  const pencil = ensurePencil(target)
  pencil.style.display = 'none'

  const show = () => {
    if (!isEditActive()) return
    positionPencil(target, pencil)
    pencil.style.display = ''
  }
  const hide = () => {
    // Cursor-bridge timeout: allow time for the user to move from the marker
    // to the pencil (or back). 250ms covers slow cursor movement across the
    // visible gap without feeling sticky after the user has moved away.
    setTimeout(() => {
      const isHoveringTarget = target.matches(':hover')
      const isHoveringPencil = pencil.matches(':hover')
      if (!isHoveringTarget && !isHoveringPencil) {
        pencil.style.display = 'none'
      }
    }, 250)
  }

  target.addEventListener('mouseenter', show)
  target.addEventListener('mouseleave', hide)
  pencil.addEventListener('mouseleave', hide)
}

function scanAndAttach(root: ParentNode): void {
  root.querySelectorAll(MARKER_SELECTOR).forEach(attachHover)
}

function cleanupRemovedMarkers(root: Element): void {
  const matches: Element[] = []
  if (root.matches(MARKER_SELECTOR)) matches.push(root)
  matches.push(...root.querySelectorAll(MARKER_SELECTOR))

  for (const target of matches) {
    // Defensive: a node may be reattached elsewhere. Only remove the
    // pencil if the target is genuinely gone from the document.
    if (document.contains(target)) continue
    const state = markers.get(target)
    state?.pencil?.remove()
    markers.delete(target)
  }
}

function initialize(): void {
  if (ctx.isPreviewMode) {
    document.body.classList.add('kcc-rs-preview')
    let stored: PreviewMode | null = null
    try {
      stored = sessionStorage.getItem(MODE_STORAGE_KEY) as PreviewMode | null
    } catch {
      /* cross-origin iframe */
    }
    setPreviewMode(stored && MODE_CYCLE.includes(stored) ? stored : 'off')
    buildFab()
  }

  scanAndAttach(document)

  const observer = new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      mutation.addedNodes.forEach((node) => {
        if (node.nodeType === Node.ELEMENT_NODE) scanAndAttach(node as Element)
      })
      mutation.removedNodes.forEach((node) => {
        if (node.nodeType === Node.ELEMENT_NODE) cleanupRemovedMarkers(node as Element)
      })
    }
  })
  // Scope the observer to document.body — not [data-kentico-editable-area-id]
  // like PageBuilderMount.ts — because resource strings can appear anywhere
  // on the page (header, footer, navigation, breadcrumbs), not only inside
  // editable areas.
  observer.observe(document.body, { childList: true, subtree: true })

  // Outside-click + Escape close the popover. Bound at module scope so the
  // listeners exist before the first popover is built.
  document.addEventListener('mousedown', (e) => {
    if (!popover || popover.el.style.display === 'none') return
    if (popover.el.contains(e.target as Node)) return
    if ((e.target as Element).closest('.kcc-rs-pencil')) return
    closePopover()
  })
  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape') return
    if (!popover || popover.el.style.display === 'none') return
    e.preventDefault()
    closePopover()
  })
  window.addEventListener('beforeunload', (e) => {
    if (popover && popover.el.style.display !== 'none' && popover.isDirty) {
      e.preventDefault()
      e.returnValue = ''
    }
  })
}

interface PopoverState {
  el: HTMLElement
  input: HTMLTextAreaElement
  languageSelect: HTMLSelectElement
  saveBtn: HTMLButtonElement
  cancelBtn: HTMLButtonElement
  errorEl: HTMLElement
  fallbackEl: HTMLElement
  currentKey: string
  currentLanguage: string
  isDirty: boolean
}

let popover: PopoverState | null = null

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize)
} else {
  initialize()
}

function buildPopover(): PopoverState {
  const el = document.createElement('div')
  el.className = 'kcc-rs-popover'
  el.setAttribute('role', 'dialog')
  el.setAttribute('aria-modal', 'false')
  el.setAttribute('aria-labelledby', 'kcc-rs-popover-key-label')
  el.innerHTML = `
    <div class="kcc-rs-popover-header">
      <strong class="kcc-rs-popover-key" id="kcc-rs-popover-key-label"></strong>
      <select class="kcc-rs-popover-lang" aria-label="Edit language"></select>
    </div>
    <textarea class="kcc-rs-popover-input" rows="3" aria-label="Resource string value"></textarea>
    <div class="kcc-rs-popover-fallback"></div>
    <div class="kcc-rs-popover-error" role="alert"></div>
    <div class="kcc-rs-popover-actions">
      <button type="button" class="kcc-rs-popover-cancel">Cancel</button>
      <button type="button" class="kcc-rs-popover-save">Save</button>
    </div>
  `
  document.body.appendChild(el)

  const state: PopoverState = {
    el,
    input: el.querySelector('.kcc-rs-popover-input') as HTMLTextAreaElement,
    languageSelect: el.querySelector('.kcc-rs-popover-lang') as HTMLSelectElement,
    saveBtn: el.querySelector('.kcc-rs-popover-save') as HTMLButtonElement,
    cancelBtn: el.querySelector('.kcc-rs-popover-cancel') as HTMLButtonElement,
    errorEl: el.querySelector('.kcc-rs-popover-error') as HTMLElement,
    fallbackEl: el.querySelector('.kcc-rs-popover-fallback') as HTMLElement,
    currentKey: '',
    currentLanguage: ctx.currentLanguage,
    isDirty: false,
  }

  // Populate language dropdown.
  for (const lang of ctx.availableLanguages) {
    const opt = document.createElement('option')
    opt.value = lang.code
    opt.textContent = lang.name
    state.languageSelect.appendChild(opt)
  }
  state.languageSelect.value = ctx.currentLanguage
  if (ctx.availableLanguages.length <= 1) {
    state.languageSelect.style.display = 'none'
  }

  state.input.addEventListener('input', () => {
    state.isDirty = true
  })
  state.cancelBtn.addEventListener('click', () => closePopover())
  state.languageSelect.addEventListener('change', async () => {
    state.currentLanguage = state.languageSelect.value
    await loadValue(state)
  })
  state.saveBtn.addEventListener('click', () => void save(state))
  state.input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      // Mirror the disabled-button gate: don't save while loadValue is in flight.
      if (state.saveBtn.disabled) return
      e.preventDefault()
      void save(state)
    }
  })

  return state
}

async function loadValue(state: PopoverState): Promise<void> {
  state.errorEl.textContent = ''
  state.input.value = ''
  state.input.placeholder = ''
  state.fallbackEl.textContent = ''
  state.saveBtn.disabled = true

  const url = `/api/resource-strings?key=${encodeURIComponent(state.currentKey)}&language=${encodeURIComponent(state.currentLanguage)}`
  try {
    const res = await fetch(url, { credentials: 'same-origin' })
    if (!res.ok) {
      state.errorEl.textContent = `Failed to load (HTTP ${res.status})`
      return
    }
    const data = (await res.json()) as {
      key: string
      language: string
      value: string | null
      fallbackValue: string
      exists: boolean
    }
    state.input.value = data.value ?? ''
    state.input.placeholder = data.fallbackValue
    if (!data.exists && data.fallbackValue && data.fallbackValue !== state.currentKey) {
      state.fallbackEl.textContent = `Fallback: ${data.fallbackValue}`
    }
    state.isDirty = false
    state.saveBtn.disabled = false
  } catch (e) {
    state.errorEl.textContent = `Network error: ${(e as Error).message}`
  }
}

function openPopover(target: Element, key: string): void {
  if (!popover) popover = buildPopover()

  // Mark all markers for this key as being edited (solid purple outline).
  // Clear any prior editing state first in case the user opened the popover
  // from a different key without explicitly closing.
  clearEditingMarkers()
  document.querySelectorAll(`[data-resource-key="${CSS.escape(key)}"]`).forEach((el) => el.classList.add('kcc-rs-editing'))

  popover.currentKey = key
  popover.currentLanguage = ctx.currentLanguage
  popover.languageSelect.value = ctx.currentLanguage
  ;(popover.el.querySelector('.kcc-rs-popover-key') as HTMLElement).textContent = key

  // Make visible first (display: '') so getBoundingClientRect can measure it
  // before we decide whether to flip above the marker.
  popover.el.style.position = 'absolute'
  popover.el.style.visibility = 'hidden'
  popover.el.style.display = ''
  popover.el.style.top = '0'
  popover.el.style.left = '0'

  const targetRect = target.getBoundingClientRect()
  const popoverRect = popover.el.getBoundingClientRect()
  const viewportHeight = window.innerHeight
  const viewportWidth = window.innerWidth

  let top = targetRect.bottom + window.scrollY + 8
  // Flip above if there isn't room below.
  if (targetRect.bottom + popoverRect.height + 8 > viewportHeight && targetRect.top - popoverRect.height - 8 > 0) {
    top = targetRect.top + window.scrollY - popoverRect.height - 8
  }
  let left = targetRect.left + window.scrollX
  // Nudge left if it would overflow the right edge.
  if (targetRect.left + popoverRect.width > viewportWidth) {
    left = Math.max(window.scrollX + 8, viewportWidth + window.scrollX - popoverRect.width - 8)
  }

  popover.el.style.top = `${top}px`
  popover.el.style.left = `${left}px`
  popover.el.style.visibility = ''

  popover.input.focus()
  void loadValue(popover)
}

function closePopover(): void {
  if (!popover) return
  popover.el.style.display = 'none'
  clearEditingMarkers()
  popover.currentKey = ''
  popover.isDirty = false
}

function clearEditingMarkers(): void {
  document.querySelectorAll('.kcc-rs-editing').forEach((el) => el.classList.remove('kcc-rs-editing'))
}

async function save(state: PopoverState): Promise<void> {
  state.errorEl.textContent = ''
  state.saveBtn.disabled = true

  const newValue = state.input.value
  // Empty input -> send null. Server interprets:
  //   - default language: null -> empty string in ResourceStringValue
  //   - non-default: null -> delete the translation row (falls back to default)
  const valueForApi = newValue.length === 0 ? null : newValue

  try {
    const res = await fetch('/api/resource-strings', {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        key: state.currentKey,
        language: state.currentLanguage,
        value: valueForApi,
      }),
    })

    if (res.status === 403) {
      state.errorEl.textContent = 'You no longer have permission to edit resource strings.'
      return
    }
    if (!res.ok) {
      state.errorEl.textContent = `Save failed (HTTP ${res.status})`
      return
    }
    const data = (await res.json()) as { key: string; language: string; value: string }

    // Update DOM only if the saved language matches the page language.
    // Known limitation: when a non-default translation is deleted (sent null,
    // server returns empty value), this clears the visible text. The page
    // really renders the default-language fallback after a reload — but the
    // response doesn't carry the fallback, so we can't show it without
    // another fetch. Acceptable for v1; revisit if the empty-flash bothers
    // editors.
    if (data.language === ctx.currentLanguage) {
      document.querySelectorAll(`[data-resource-key="${CSS.escape(data.key)}"]`).forEach((el) => {
        el.textContent = data.value
      })
    }

    closePopover()
  } catch (e) {
    state.errorEl.textContent = `Network error: ${(e as Error).message}`
  } finally {
    state.saveBtn.disabled = false
  }
}
