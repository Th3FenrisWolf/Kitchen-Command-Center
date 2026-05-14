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

// Kentico xp-eye-slash
const ICON_OFF: { d: string; clipRule?: string }[] = [
  {
    d: 'M14.833 1.885a.532.532 0 0 0-.001-.733.487.487 0 0 0-.707 0l-1.742 1.81c-1.152-.831-2.602-1.444-4.38-1.444-2.817 0-4.818 1.537-6.092 3.03A12.244 12.244 0 0 0 .16 7.278a7.358 7.358 0 0 0-.11.25l-.006.014-.002.005v.002s-.038.082-.038.197L.04 7.55a.536.536 0 0 0 0 .392l-.037-.196c0 .115.037.196.037.196v.002l.002.002.003.008.01.025.039.09A12.044 12.044 0 0 0 .84 9.472c.534.86 1.355 1.951 2.497 2.842a.477.477 0 0 0 .026.018L1.65 14.115a.532.532 0 0 0 0 .733.487.487 0 0 0 .707 0l3.82-3.968a.503.503 0 0 0 .112-.117l4.107-4.267a.504.504 0 0 0 .155-.161l4.283-4.45Zm-4.743 3.46 1.574-1.636c-.987-.677-2.195-1.154-3.66-1.154-2.432 0-4.181 1.318-5.344 2.68a11.207 11.207 0 0 0-1.598 2.489l-.01.022.085.179c.118.244.3.59.545.985.494.796 1.24 1.781 2.257 2.575.048.037.087.08.117.129l1.142-1.187a3.721 3.721 0 0 1-.693-2.168c0-2.005 1.566-3.63 3.5-3.63.78 0 1.501.269 2.085.716ZM5.916 9.681 9.373 6.09a2.434 2.434 0 0 0-1.369-.424c-1.38 0-2.5 1.16-2.5 2.593 0 .522.152 1.011.412 1.423Z',
    clipRule: 'evenodd',
  },
  {
    d: 'M14.37 5.708a.49.49 0 0 1 .694.146 12.276 12.276 0 0 1 .843 1.56l.043.1.012.029.003.008.001.003v.001s.037.053.037.196c0 .143-.037.196-.037.196v.002l-.002.005-.006.015a10.993 10.993 0 0 1-.452.932 12.28 12.28 0 0 1-1.409 2.056c-1.274 1.497-3.274 3.039-6.093 3.039-.418 0-.82-.034-1.203-.098a.517.517 0 0 1-.416-.593.503.503 0 0 1 .572-.43c.332.054.68.084 1.047.084 2.43 0 4.18-1.322 5.344-2.688a11.25 11.25 0 0 0 1.597-2.497l.01-.023a11.236 11.236 0 0 0-.726-1.324.53.53 0 0 1 .142-.719Z',
  },
  {
    d: 'M11.504 8.26a.51.51 0 0 0-.5-.52.51.51 0 0 0-.5.52c0 1.432-1.118 2.592-2.5 2.592a.51.51 0 0 0-.5.518.51.51 0 0 0 .5.519c1.934 0 3.5-1.624 3.5-3.63Z',
  },
]

// Kentico xp-eye
const ICON_VIEW: { d: string; clipRule?: string }[] = [
  {
    d: 'M8 4.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7ZM5.5 8a2.5 2.5 0 1 1 5 0 2.5 2.5 0 0 1-5 0Z',
    clipRule: 'evenodd',
  },
  {
    d: 'M15.964 7.809s.037.086.037.19c0 .134-.037.19-.037.19v.001l-.003.005-.006.015a4.986 4.986 0 0 1-.11.24c-.075.16-.19.386-.342.655a11.79 11.79 0 0 1-1.409 1.975c-1.274 1.438-3.275 2.918-6.093 2.918-2.819 0-4.82-1.48-6.094-2.918A11.787 11.787 0 0 1 .156 8.45a6.963 6.963 0 0 1-.11-.24L.04 8.195.038 8.19V8.19S0 8.11 0 7.999c0-.112.038-.19.038-.19v-.002l.002-.005.007-.015a4.177 4.177 0 0 1 .109-.24 11.786 11.786 0 0 1 1.752-2.63C3.181 3.479 5.181 1.999 8 1.999c2.818 0 4.819 1.48 6.093 2.918a11.788 11.788 0 0 1 1.752 2.63 6.979 6.979 0 0 1 .11.24l.005.015.002.005.001.002ZM1.368 8.61a9.445 9.445 0 0 1-.31-.592L1.048 8l.01-.021A10.787 10.787 0 0 1 2.655 5.58C3.82 4.267 5.57 2.998 8.002 2.998s4.182 1.269 5.345 2.581a10.784 10.784 0 0 1 1.598 2.398l.01.02-.01.021a10.786 10.786 0 0 1-1.598 2.397c-1.163 1.313-2.913 2.582-5.345 2.582s-4.182-1.269-5.345-2.581A10.789 10.789 0 0 1 1.368 8.61Z',
    clipRule: 'evenodd',
  },
]

// Kentico xp-edit
const ICON_EDIT: { d: string; clipRule?: string }[] = [
  {
    d: 'M1 14.993v-4.941l8.471-8.47a2 2 0 0 1 2.828 0l2.114 2.114a2 2 0 0 1 0 2.828l-8.47 8.469H1Zm4.528-1 5.23-5.228L7.23 5.237 2 10.466v3.527h3.528ZM7.937 4.53l3.528 3.528 2.241-2.24a1 1 0 0 0 0-1.415l-2.114-2.114a1 1 0 0 0-1.414 0L7.937 4.53Z',
    clipRule: 'evenodd',
  },
  {
    d: 'M9.49 13.998a.5.5 0 0 0 0 1h4.996a.5.5 0 0 0 0-1H9.49Z',
  },
]

// Kentico xp-translate (Resource Strings module icon)
const ICON_TRANSLATE: { d: string; clipRule?: string }[] = [
  {
    d: 'M5.993 3.006a.5.5 0 0 1 .464.314l2 5.003a.5.5 0 0 1-.929.371L6.851 7H5.136l-.678 1.695a.5.5 0 1 1-.928-.371l2-5.003a.5.5 0 0 1 .463-.314Zm0 1.847.458 1.146h-.916l.458-1.146Z',
    clipRule: 'evenodd',
  },
  {
    d: 'M-.003 6.007A6 6 0 0 1 5.993.004 6 6 0 0 1 11.99 6.06a5.016 5.016 0 0 1 3.154 7.713l.197.985a.5.5 0 0 1-.588.589l-.984-.197a5.012 5.012 0 0 1-7.701-3.14h-.075a5.951 5.951 0 0 1-3.37-1.045l-1.244.249a.5.5 0 0 1-.587-.59l.249-1.244A5.963 5.963 0 0 1-.003 6.007Zm7.073 5.907a4.013 4.013 0 0 0 6.29 2.301.499.499 0 0 1 .395-.088l.459.092-.092-.459a.501.501 0 0 1 .088-.395 4.016 4.016 0 0 0-2.313-6.3 5.967 5.967 0 0 1-.707 1.94h.47a.53.53 0 0 1 .019-.001H12.502a.5.5 0 0 1 0 1h-.356a3.9 3.9 0 0 1-.67 1.75c.26.162.554.252.879.252a.5.5 0 0 1 0 1 2.642 2.642 0 0 1-1.587-.526 2.653 2.653 0 0 1-1.588.526.5.5 0 0 1 0-1c.321 0 .617-.09.879-.252a3.742 3.742 0 0 1-.48-.934 5.964 5.964 0 0 1-2.509 1.094Zm3.397-1.91-.058.065c.066.342.19.664.358.943.18-.299.307-.645.369-1.007h-.669Zm-4.474-9A5 5 0 0 0 .996 6.007c0 1.11.365 2.132.979 2.963a.5.5 0 0 1 .088.396l-.144.72.72-.143a.5.5 0 0 1 .395.088c.83.613 1.85.979 2.96.979a5 5 0 0 0 4.997-5.003 5 5 0 0 0-4.998-5.003Z',
    clipRule: 'evenodd',
  },
]

const MODE_ICONS: Record<PreviewMode, { d: string; clipRule?: string }[]> = {
  off: ICON_OFF,
  view: ICON_VIEW,
  edit: ICON_EDIT,
}

function createPencilIcon(): SVGSVGElement {
  return createSvgIcon(ICON_EDIT, 16)
}

function createSvgIcon(paths: { d: string; clipRule?: string }[], size: number): SVGSVGElement {
  const svg = document.createElementNS(SVG_NS, 'svg')
  svg.setAttribute('viewBox', '0 0 16 16')
  svg.setAttribute('width', String(size))
  svg.setAttribute('height', String(size))
  svg.setAttribute('fill', 'currentColor')
  svg.setAttribute('aria-hidden', 'true')
  for (const icon of paths) {
    const path = document.createElementNS(SVG_NS, 'path')
    path.setAttribute('d', icon.d)
    if (icon.clipRule) {
      path.setAttribute('fill-rule', 'evenodd')
      path.setAttribute('clip-rule', 'evenodd')
    }
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

function buildFab(): void {
  const fab = document.createElement('button')
  fab.type = 'button'
  fab.className = 'kcc-rs-mode-fab'

  fab.appendChild(createSvgIcon(ICON_TRANSLATE, 20))

  const label = document.createElement('span')
  label.className = 'kcc-rs-fab-label'
  label.textContent = MODE_LABELS[currentMode]
  fab.appendChild(label)

  const modeIconWrap = document.createElement('span')
  modeIconWrap.className = 'kcc-rs-fab-mode-icon'
  modeIconWrap.appendChild(createSvgIcon(MODE_ICONS[currentMode], 16))
  fab.appendChild(modeIconWrap)

  fab.setAttribute('aria-label', MODE_LABELS[currentMode])
  document.body.appendChild(fab)

  fab.addEventListener('click', () => {
    const idx = MODE_CYCLE.indexOf(currentMode)
    const next = MODE_CYCLE[(idx + 1) % MODE_CYCLE.length] ?? 'off'
    setPreviewMode(next)
    label.textContent = MODE_LABELS[currentMode]
    modeIconWrap.replaceChildren(createSvgIcon(MODE_ICONS[currentMode], 16))
    fab.setAttribute('aria-label', MODE_LABELS[currentMode])
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
  pencil.appendChild(createPencilIcon())

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
    pencil.style.display = 'inline-flex'
  }
  const hide = () => {
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
