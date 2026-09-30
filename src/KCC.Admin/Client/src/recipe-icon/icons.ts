export function iconLabel(icon: string): string {
  return icon.replace(/^fa-[a-z]+\s+fa-/, '')
}

export function filterIcons(icons: string[], search: string): string[] {
  const term = search.trim().toLowerCase()
  return term === '' ? icons : icons.filter((icon) => iconLabel(icon).includes(term))
}
