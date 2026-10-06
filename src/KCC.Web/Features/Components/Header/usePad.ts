import { nextTick, ref } from 'vue'

export type CardId = 'recipes' | 'kitchen' | 'search' | 'find' | 'menu'

export type CardState = 'rest' | 'peek' | 'open'

export interface PadElements {
  header(): HTMLElement | null
  row(): HTMLElement | null
  menu(id: CardId): HTMLElement | null
  card(id: CardId): HTMLElement | null
  searchField(): HTMLElement | null
}

const CARDS: CardId[] = ['recipes', 'kitchen', 'search', 'find', 'menu']

const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled])'

// The header renders both rows and every card, and CSS shows one set: only what has a box can take focus.
const focusables = (root: HTMLElement | null) =>
  root ? [...root.querySelectorAll<HTMLElement>(FOCUSABLE)].filter((element) => element.getClientRects().length > 0) : []

// `/` is a character in a field, and a modal dialog (cook mode) owns the keyboard while it is open.
const keepsSlash = (target: EventTarget | null) =>
  Boolean((target as Element | null)?.closest?.('input, textarea, select, [contenteditable="true"], [aria-modal="true"]'))

export function usePad(elements: PadElements) {
  const open = ref<CardId | null>(null)
  const peeking = ref<CardId | null>(null)
  const swapping = ref(false)

  const stateOf = (id: CardId): CardState => (open.value === id ? 'open' : peeking.value === id ? 'peek' : 'rest')

  function peek(id: CardId) {
    if (open.value !== id) {
      peeking.value = id
    }
  }

  function unpeek(id: CardId) {
    if (peeking.value === id) {
      peeking.value = null
    }
  }

  function show(id: CardId) {
    swapping.value = open.value !== null && open.value !== id
    open.value = id
    peeking.value = null
  }

  function close() {
    open.value = null
    peeking.value = null
    swapping.value = false
  }

  function toggle(id: CardId) {
    if (open.value === id) {
      close()
    } else {
      show(id)
    }
  }

  async function focusInto(id: CardId) {
    await nextTick()
    focusables(elements.card(id))[0]?.focus({ preventScroll: true })
  }

  function onMenuKeydown(id: CardId, event: KeyboardEvent) {
    if (event.key === 'ArrowDown' && !event.shiftKey && !event.ctrlKey && !event.metaKey) {
      event.preventDefault()
      show(id)
      void focusInto(id)
    } else if (event.key === 'Tab' && !event.shiftKey && open.value === id) {
      const first = focusables(elements.card(id))[0]
      if (first) {
        event.preventDefault()
        first.focus({ preventScroll: true })
      }
    }
  }

  function onCardKeydown(id: CardId, event: KeyboardEvent) {
    if (event.key !== 'Tab') {
      return
    }

    const inside = focusables(elements.card(id))
    const at = inside.indexOf(event.target as HTMLElement)
    if (event.shiftKey && at === 0) {
      event.preventDefault()
      elements.menu(id)?.focus({ preventScroll: true })
    } else if (!event.shiftKey && at === inside.length - 1) {
      const row = focusables(elements.row())
      const menu = elements.menu(id)
      const next = menu ? row[row.indexOf(menu) + 1] : undefined
      close()
      if (next) {
        event.preventDefault()
        next.focus({ preventScroll: true })
      }
    }
  }

  // A focus move with no destination is a click on something that cannot take focus, so it leaves the card be.
  function onFocusOut(event: FocusEvent) {
    const id = open.value
    const to = event.relatedTarget as Node | null
    if (!id || !to || elements.card(id)?.contains(to) || CARDS.some((card) => elements.menu(card) === to)) {
      return
    }

    close()
  }

  function onDocumentClick(event: MouseEvent) {
    if (!elements.header()?.contains(event.target as Node)) {
      close()
    }
  }

  async function focusSearch() {
    const field = elements.searchField()
    if (field) {
      field.focus({ preventScroll: true })
    } else if (elements.card('find')) {
      show('find')
      await focusInto('find')
    }
  }

  function onDocumentKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && open.value) {
      const id = open.value
      elements.menu(id)?.focus({ preventScroll: true })
      close()
    } else if (event.key === '/' && !event.ctrlKey && !event.metaKey && !event.altKey && !keepsSlash(event.target)) {
      event.preventDefault()
      void focusSearch()
    }
  }

  return {
    open,
    peeking,
    swapping,
    stateOf,
    peek,
    unpeek,
    show,
    close,
    toggle,
    onMenuKeydown,
    onCardKeydown,
    onFocusOut,
    onDocumentClick,
    onDocumentKeydown,
  }
}
