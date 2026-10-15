const THIS_TAB_TARGETS = ['', '_self', '_parent', '_top']

const withoutFragment = (url: string) => url.split('#')[0]

const isPlainPrimaryClick = (event: MouseEvent) =>
  event.button === 0 && !event.metaKey && !event.ctrlKey && !event.shiftKey && !event.altKey

const linkAround = (target: EventTarget | null): HTMLAnchorElement | null =>
  target && 'closest' in target ? (target as Element).closest<HTMLAnchorElement>('a[href]') : null

const opensInThisTab = (link: HTMLAnchorElement) =>
  !link.hasAttribute('download') && THIS_TAB_TARGETS.includes(link.target.toLowerCase())

export function guardedLink(event: MouseEvent, page: string): HTMLAnchorElement | null {
  if (event.defaultPrevented || !isPlainPrimaryClick(event)) return null

  const link = linkAround(event.target)
  if (!link || !opensInThisTab(link)) return null

  const here = new URL(page)
  if (link.origin !== here.origin) return null

  const fragmentOfThisPage = link.href.includes('#') && withoutFragment(link.href) === withoutFragment(here.href)
  return fragmentOfThisPage ? null : link
}

export function controlOfInertAncestor(link: Element): HTMLElement | null {
  const region = link.closest('[inert]')
  if (!region?.id) return null

  const controls = [...link.ownerDocument.querySelectorAll<HTMLElement>('[aria-controls]')]
  return controls.find((control) => control.getAttribute('aria-controls')?.split(/\s+/).includes(region.id)) ?? null
}
