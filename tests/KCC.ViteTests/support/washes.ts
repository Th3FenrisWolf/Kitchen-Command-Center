export interface RenderedWash {
  id: string
  style: string
}

const FIELD =
  /<span class="kcc-washes"[^>]*>(?:<!--\[-->)?((?:<span class="kcc-wash"[^>]*><\/span>)*)(?:<!--\]-->)?<\/span>/g
const WASH = /<span class="kcc-wash" data-wash="([0-9a-f]{8})" style="([^"]*)"><\/span>/g

export const washFields = (html: string): RenderedWash[][] =>
  [...html.matchAll(FIELD)].map(([, washes]) => [...washes.matchAll(WASH)].map(([, id, style]) => ({ id, style })))

export const inkOf = (wash: RenderedWash | undefined) => wash?.style.match(/--wash-ink:([^;]+)/)?.[1]

export const chosenInk = (wash: string) => `var(--color-${wash})`
