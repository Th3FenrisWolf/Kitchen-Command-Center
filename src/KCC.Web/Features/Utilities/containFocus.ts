const FOCUSABLE = 'button:not([disabled]), [href], input, [tabindex]:not([tabindex="-1"])'

export function containFocus(event: KeyboardEvent, root: HTMLElement | null | undefined): void {
  if (!root) return
  const focusables = root.querySelectorAll<HTMLElement>(FOCUSABLE)
  if (focusables.length === 0) return
  const first = focusables[0]!
  const last = focusables[focusables.length - 1]!
  const active = document.activeElement
  if (event.shiftKey && (active === first || active === root)) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && active === last) {
    event.preventDefault()
    first.focus()
  }
}
