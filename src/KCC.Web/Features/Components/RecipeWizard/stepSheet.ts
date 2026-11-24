// 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
export const STEP_PAD = 'clamp(24px, 7.5vw, 48px)'

export const stepNumber = (index: number): string => String(index + 1).padStart(2, '0')

export const markId = (controlId: string): string => `${controlId}-mark`

export const describedBy = (...ids: (string | false | undefined)[]): string | undefined =>
  ids.filter(Boolean).join(' ') || undefined

export function focusHeading(heading: HTMLElement | null | undefined): void {
  if (!heading) return

  heading.tabIndex = -1
  heading.focus()
}

// Removing the focused control would make the browser restyle the whole step before the swap can paint.
export function letGoOfFocusIn(leaving: HTMLElement | null | undefined): void {
  const focused = document.activeElement
  if (focused instanceof HTMLElement && leaving?.contains(focused)) focused.blur()
}

// A focus() in the frame that swaps the step forces a second restyle before that frame can paint.
export const afterNextPaint = (): Promise<void> => new Promise((resolve) => requestAnimationFrame(() => setTimeout(resolve)))
