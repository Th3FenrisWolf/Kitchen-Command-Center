export const fill = (template: string, ...words: string[]): string =>
  template.replace(/\{(\d+)\}/g, (placeholder, index: string) => words[Number(index)] ?? placeholder)

// The day is a calendar date, so it is printed in UTC and in the site's one language, alike on the server and the client.
const LONG_DAY = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'long', day: 'numeric', timeZone: 'UTC' })

export const longDay = (isoDay: string): string => {
  const day = new Date(`${isoDay}T00:00:00Z`)
  return Number.isNaN(day.getTime()) ? isoDay : LONG_DAY.format(day)
}
