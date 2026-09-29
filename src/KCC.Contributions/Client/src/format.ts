export const ratingSteps = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5]

export const maxTextLength = 4000

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) {
    return ''
  }

  // The server writes UTC; a timestamp without an offset would otherwise be read as local time.
  const date = new Date(/Z$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`)
  return Number.isNaN(date.getTime())
    ? ''
    : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

export function formatRating(rating: number): string {
  return `${rating.toFixed(1)} ★`
}

export function totalPagesFor(total: number, pageSize: number): number {
  return Math.max(1, Math.ceil(Math.max(0, total) / Math.max(1, pageSize)))
}
