// 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
export const STEP_PAD = 'clamp(24px, 7.5vw, 48px)'

export const stepNumber = (index: number): string => String(index + 1).padStart(2, '0')

export const markId = (controlId: string): string => `${controlId}-mark`

export function focusHeading(heading: HTMLElement | null | undefined): void {
  if (!heading) return

  heading.tabIndex = -1
  heading.focus()
}
