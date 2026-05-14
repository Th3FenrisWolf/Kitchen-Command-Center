interface GridLayout {
  rows: number
  columns: number
  spans: Record<string, { colSpan: number; rowSpan: number }>
}

;(function () {
  const EDITOR_SELECTOR = '[data-bento-box-editor]'

  function initEditor(editor: Element) {
    if ((editor as HTMLElement).dataset.bentoBoxBound) return
    ;(editor as HTMLElement).dataset.bentoBoxBound = '1'

    const pageId = editor.getAttribute('data-page-id')
    const language = editor.getAttribute('data-language')
    const sectionType = editor.getAttribute('data-section-type')

    function getLayout(): GridLayout {
      try {
        return JSON.parse(editor.getAttribute('data-grid-layout') || '{}')
      } catch {
        return { rows: 1, columns: 1, spans: {} }
      }
    }

    async function saveLayout(layout: GridLayout) {
      setButtonsDisabled(true)
      try {
        const res = await fetch('/api/bento-box/property', {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            pageId: Number(pageId),
            languageName: language,
            sectionType,
            propertyName: 'gridLayout',
            value: JSON.stringify(layout),
          }),
        })

        if (!res.ok) {
          console.error('[BentoBox] API error:', res.status, await res.text())
          setButtonsDisabled(false)
          return
        }

        window.location.reload()
      } catch (err) {
        console.error('[BentoBox] Fetch failed:', err)
        setButtonsDisabled(false)
      }
    }

    function setButtonsDisabled(disabled: boolean) {
      editor.querySelectorAll<HTMLButtonElement>('[data-bento-action]').forEach((btn) => {
        btn.disabled = disabled
      })
    }

    function truncateSpans(layout: GridLayout) {
      const valid: typeof layout.spans = {}
      for (const [key, span] of Object.entries(layout.spans)) {
        const match = key.match(/^r(\d+)c(\d+)$/)
        if (!match) continue
        const r = parseInt(match[1]!, 10)
        const c = parseInt(match[2]!, 10)
        if (r >= layout.rows || c >= layout.columns) continue

        span.colSpan = Math.min(span.colSpan, layout.columns - c)
        span.rowSpan = Math.min(span.rowSpan, layout.rows - r)

        if (span.colSpan > 1 || span.rowSpan > 1) {
          valid[key] = span
        }
      }
      layout.spans = valid
    }

    function buildOccupancy(layout: GridLayout): { owner: string[][] } {
      const owner: string[][] = Array.from({ length: layout.rows }, () =>
        Array.from({ length: layout.columns }, () => ''),
      )

      for (const [key, span] of Object.entries(layout.spans)) {
        const match = key.match(/^r(\d+)c(\d+)$/)
        if (!match) continue
        const r = parseInt(match[1]!, 10)
        const c = parseInt(match[2]!, 10)
        const rs = span.rowSpan || 1
        const cs = span.colSpan || 1
        for (let ri = r; ri < r + rs && ri < layout.rows; ri++) {
          for (let ci = c; ci < c + cs && ci < layout.columns; ci++) {
            owner[ri]![ci] = key
          }
        }
      }

      for (let r = 0; r < layout.rows; r++) {
        for (let c = 0; c < layout.columns; c++) {
          if (!owner[r]![c]) {
            owner[r]![c] = `r${r}c${c}`
          }
        }
      }

      return { owner }
    }

    function canExpandRight(layout: GridLayout, cellKey: string): boolean {
      const match = cellKey.match(/^r(\d+)c(\d+)$/)
      if (!match) return false
      const r = parseInt(match[1]!, 10)
      const c = parseInt(match[2]!, 10)
      const span = layout.spans[cellKey] || { colSpan: 1, rowSpan: 1 }
      const newCol = c + span.colSpan
      if (newCol >= layout.columns) return false

      const { owner } = buildOccupancy(layout)
      for (let ri = r; ri < r + span.rowSpan; ri++) {
        if (owner[ri]![newCol] !== `r${ri}c${newCol}`) return false
      }
      return true
    }

    function canExpandDown(layout: GridLayout, cellKey: string): boolean {
      const match = cellKey.match(/^r(\d+)c(\d+)$/)
      if (!match) return false
      const r = parseInt(match[1]!, 10)
      const c = parseInt(match[2]!, 10)
      const span = layout.spans[cellKey] || { colSpan: 1, rowSpan: 1 }
      const newRow = r + span.rowSpan
      if (newRow >= layout.rows) return false

      const { owner } = buildOccupancy(layout)
      for (let ci = c; ci < c + span.colSpan; ci++) {
        if (owner[newRow]![ci] !== `r${newRow}c${ci}`) return false
      }
      return true
    }

    editor.addEventListener(
      'click',
      (e) => {
      const target = (e.target as HTMLElement).closest<HTMLButtonElement>('[data-bento-action]')
      if (!target) return

      e.stopPropagation()
      e.preventDefault()

      const action = target.dataset.bentoAction
      const cellKey = target.dataset.cell
      const layout = getLayout()

      switch (action) {
        case 'add-column':
          if (layout.columns < 6) layout.columns++
          break
        case 'remove-column':
          if (layout.columns > 1) {
            layout.columns--
            truncateSpans(layout)
          }
          break
        case 'add-row':
          if (layout.rows < 6) layout.rows++
          break
        case 'remove-row':
          if (layout.rows > 1) {
            layout.rows--
            truncateSpans(layout)
          }
          break
        case 'expand-right': {
          if (!cellKey || !canExpandRight(layout, cellKey)) return
          const span = layout.spans[cellKey] || { colSpan: 1, rowSpan: 1 }
          span.colSpan++
          layout.spans[cellKey] = span
          break
        }
        case 'shrink-right': {
          if (!cellKey) return
          const span = layout.spans[cellKey]
          if (!span || span.colSpan <= 1) return
          span.colSpan--
          if (span.colSpan <= 1 && span.rowSpan <= 1) {
            delete layout.spans[cellKey]
          }
          break
        }
        case 'expand-down': {
          if (!cellKey || !canExpandDown(layout, cellKey)) return
          const span = layout.spans[cellKey] || { colSpan: 1, rowSpan: 1 }
          span.rowSpan++
          layout.spans[cellKey] = span
          break
        }
        case 'shrink-down': {
          if (!cellKey) return
          const span = layout.spans[cellKey]
          if (!span || span.rowSpan <= 1) return
          span.rowSpan--
          if (span.colSpan <= 1 && span.rowSpan <= 1) {
            delete layout.spans[cellKey]
          }
          break
        }
        default:
          return
      }

      saveLayout(layout)
    },
      true,
    )
  }

  function scanAndInit() {
    document.querySelectorAll(EDITOR_SELECTOR).forEach(initEditor)
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', scanAndInit)
  } else {
    scanAndInit()
  }

  new MutationObserver((mutations) => {
    for (const mutation of mutations) {
      for (const node of mutation.addedNodes) {
        if (node.nodeType !== Node.ELEMENT_NODE) continue
        const el = node as Element
        if (el.matches(EDITOR_SELECTOR)) initEditor(el)
        el.querySelectorAll(EDITOR_SELECTOR).forEach(initEditor)
      }
    }
  }).observe(document.body, { childList: true, subtree: true })
})()
