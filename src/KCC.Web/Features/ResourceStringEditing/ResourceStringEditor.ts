// Inline resource string editor — loaded only in page builder edit mode.
// Scans for [data-resource-key] elements, attaches a hover pencil overlay,
// and (in later tasks) opens a popover and saves edits.

import './ResourceStringEditor.css'

interface ResourceStringEditorContext {
  currentLanguage: string
  availableLanguages: { code: string; name: string }[]
}

function readContext(): ResourceStringEditorContext {
  const el = document.getElementById('kcc-rs-editor-context')
  if (!el?.textContent) {
    console.warn('[rs-editor] kcc-rs-editor-context script tag missing; popover will fail to load values.')
    return { currentLanguage: '', availableLanguages: [] }
  }
  try {
    const parsed = JSON.parse(el.textContent)
    return {
      currentLanguage: typeof parsed?.currentLanguage === 'string' ? parsed.currentLanguage : '',
      availableLanguages: Array.isArray(parsed?.availableLanguages) ? parsed.availableLanguages : [],
    }
  } catch {
    console.warn('[rs-editor] kcc-rs-editor-context script tag is not valid JSON; popover will fail to load values.')
    return { currentLanguage: '', availableLanguages: [] }
  }
}

const ctx = readContext()

const MARKER_SELECTOR = '[data-resource-key]'
const PENCIL_CLASS = 'kcc-rs-pencil'

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

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize)
} else {
  initialize()
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

  state.input.addEventListener('input', () => { state.isDirty = true })
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
    const data = await res.json() as {
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
  document
    .querySelectorAll(`[data-resource-key="${CSS.escape(key)}"]`)
    .forEach((el) => el.classList.add('kcc-rs-editing'))

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
  document
    .querySelectorAll('.kcc-rs-editing')
    .forEach((el) => el.classList.remove('kcc-rs-editing'))
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
    const data = await res.json() as { key: string; language: string; value: string }

    // Update DOM only if the saved language matches the page language.
    // Known limitation: when a non-default translation is deleted (sent null,
    // server returns empty value), this clears the visible text. The page
    // really renders the default-language fallback after a reload — but the
    // response doesn't carry the fallback, so we can't show it without
    // another fetch. Acceptable for v1; revisit if the empty-flash bothers
    // editors.
    if (data.language === ctx.currentLanguage) {
      document
        .querySelectorAll(`[data-resource-key="${CSS.escape(data.key)}"]`)
        .forEach((el) => {
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
