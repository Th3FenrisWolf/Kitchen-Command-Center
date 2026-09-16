import { hatch, rectPath } from '~/Ink/inkPaths'

export type InkKind = 'sheet' | 'card' | 'tile' | 'button'

export interface InkOptions {
  kind?: InkKind
  /** How far the frame extends past the element's box, in px. */
  inset?: number
  /** Corner radius of the drawn rectangle before wobble, in px. */
  radius?: number
  /** Wobble amplitude, in px. */
  amp?: number
  /** Stroke width, in px. */
  sw?: number
  /** Any CSS colour; defaults to the kind's ink. */
  color?: string
  /** Fixes the wobble; otherwise each element gets its own. */
  seed?: number
  /** Stroke opacity, 0–1. */
  op?: number
  /** Adds a second, looser pass outside the first. */
  double?: boolean
  /** Draws the hatched shadow strip under the element. */
  hatch?: boolean
}

export type InkBinding = InkOptions | InkKind | true | null | undefined

export interface ResolvedInk {
  kind: InkKind
  inset: number
  radius: number
  amp: number
  sw: number
  color: string
  seed: number | undefined
  op: number
  double: boolean
  hatch: boolean
}

/* Per-kind defaults, lifted from the design's ink.js AUTO table. */
export const INK_KINDS: Record<InkKind, Pick<ResolvedInk, 'inset' | 'radius' | 'amp' | 'sw' | 'color' | 'hatch'>> = {
  sheet: { inset: 7, radius: 16, amp: 2.1, sw: 4.8, color: 'var(--color-ink-line)', hatch: true },
  card: { inset: 7, radius: 16, amp: 2.1, sw: 4.8, color: 'var(--color-ink-line)', hatch: false },
  tile: { inset: 5, radius: 14, amp: 1.5, sw: 4.4, color: 'var(--color-ink-on-wash)', hatch: false },
  button: { inset: 6, radius: 999, amp: 1.5, sw: 4.4, color: 'var(--color-ink-on-wash)', hatch: false },
}

/**
 * A key overrides the kind's default only when it carries a usable value. Callers pass `undefined` for
 * "use the default", and a non-finite number has to mean the same thing: `rectPath` sizes its point loop
 * from the perimeter, so a NaN `inset`, `radius` or `amp` collapses that loop to zero points and
 * `closedSpline` then dereferences an empty array. The throw would escape `mountInk`'s forEach and abort
 * every later element. `v-ink` bindings arrive here without passing `mountInk`'s attribute parsing, so
 * this is the one choke point both entry points share.
 */
const overrides = ([, value]: [string, unknown]) =>
  value !== undefined && (typeof value !== 'number' || Number.isFinite(value))

export function resolveInk(binding: InkBinding): ResolvedInk | null {
  if (binding === null || binding === undefined) return null
  const options: InkOptions = binding === true ? {} : typeof binding === 'string' ? { kind: binding } : binding
  const kind = options.kind ?? 'sheet'
  const defined = Object.fromEntries(Object.entries(options).filter(overrides)) as Partial<InkOptions>
  return { ...INK_KINDS[kind], op: 1, double: false, seed: undefined, ...defined, kind }
}

const NS = 'http://www.w3.org/2000/svg'
const KINDS: InkKind[] = ['sheet', 'card', 'tile', 'button']

let seedCounter = 0
const seeds = new WeakMap<Element, number>()
const seedOf = (el: Element) => {
  let seed = seeds.get(el)
  if (seed === undefined) {
    seed = (seedCounter += 9) + 3
    seeds.set(el, seed)
  }
  return seed
}

function svgChild(el: HTMLElement, className: string, role: string): SVGSVGElement {
  let svg = el.querySelector<SVGSVGElement>(`:scope > svg.${className}[data-role="${role}"]`)
  if (!svg) {
    svg = document.createElementNS(NS, 'svg')
    svg.setAttribute('class', className)
    svg.dataset.role = role
    svg.setAttribute('preserveAspectRatio', 'none')
    svg.setAttribute('aria-hidden', 'true')
    el.appendChild(svg)
  }
  return svg
}

function paintFrame(el: HTMLElement, o: ResolvedInk, role: 'main' | 'double') {
  const svg = svgChild(el, 'sk-inkframe', role)
  const looser = role === 'double'
  const inset = o.inset + (looser ? 7 : 0)
  const rect = el.getBoundingClientRect()
  const w = Math.round(rect.width) + inset * 2
  const h = Math.round(rect.height) + inset * 2
  // NaN fails every comparison, so `w < 10` would wave a NaN dimension through into rectPath, where it
  // collapses the point loop and throws out of closedSpline. `!(w >= 10)` rejects it. Do not "simplify".
  if (!(w >= 10) || !(h >= 10)) return
  svg.style.setProperty('--o', `${inset}px`)
  svg.style.setProperty('--sw', String(looser ? 1.5 : o.sw))
  svg.style.transform = looser ? 'rotate(0.3deg)' : ''
  svg.setAttribute('viewBox', `0 0 ${w} ${h}`)
  let path = svg.querySelector('path')
  if (!path) {
    path = document.createElementNS(NS, 'path')
    svg.appendChild(path)
  }
  const seed = (o.seed ?? seedOf(el)) + (looser ? 17 : 0)
  path.setAttribute('d', rectPath(w, h, o.radius + (looser ? 6 : 0), looser ? 2.6 : o.amp, seed))
  path.style.stroke = o.color
  path.setAttribute('opacity', String(looser ? 0.45 : o.op))
  svg.dataset.drawn = ''
}

function paintHatch(el: HTMLElement, o: ResolvedInk) {
  const svg = svgChild(el, 'sk-hatch', 'hatch')
  // The strip's size comes from Ink.css, so measure the svg itself.
  const rect = svg.getBoundingClientRect()
  const w = Math.round(rect.width)
  const h = Math.round(rect.height)
  // NaN-safe, as in paintFrame.
  if (!(w >= 10) || !(h >= 4)) return
  svg.setAttribute('viewBox', `0 0 ${w} ${h}`)
  const seed = (o.seed ?? seedOf(el)) + 5
  svg.replaceChildren(
    ...hatch(w, h, { seed, gap: 6.5, amp: 0.8 }).map((d) => {
      const path = document.createElementNS(NS, 'path')
      path.setAttribute('d', d)
      return path
    }),
  )
  svg.dataset.drawn = ''
}

export function paintInk(el: HTMLElement, o: ResolvedInk) {
  if (getComputedStyle(el).position === 'static') el.style.position = 'relative'
  paintFrame(el, o, 'main')
  if (o.double) paintFrame(el, o, 'double')
  if (o.hatch) paintHatch(el, o)
}

/**
 * Paints now, again after the next frame and once the fonts settle, and on every resize. Returns a
 * dispose function that removes the drawing.
 */
export function attachInk(el: HTMLElement, binding: InkBinding): () => void {
  const o = resolveInk(binding)
  if (!o) return () => {}
  let disposed = false
  const paint = () => {
    if (!disposed) paintInk(el, o)
  }
  paint()
  const frame = requestAnimationFrame(paint)
  document.fonts?.ready.then(paint)
  const observer = new ResizeObserver(paint)
  observer.observe(el)
  return () => {
    disposed = true
    cancelAnimationFrame(frame)
    observer.disconnect()
    el.querySelectorAll(':scope > svg.sk-inkframe, :scope > svg.sk-hatch').forEach((svg) => svg.remove())
  }
}

const mounted = new WeakSet<Element>()

/**
 * Razor markup cannot use the directive, so it opts in with `data-ink="sheet|card|tile|button"` and the
 * design's `data-ink-*` tuning attributes (`data-ink-inset|radius|amp|sw|color|seed|op`, `data-ink-double`,
 * `data-ink-hatch="off"`). Safe to call repeatedly.
 */
export function mountInk(root: ParentNode) {
  root.querySelectorAll<HTMLElement>('[data-ink]').forEach((el) => {
    if (mounted.has(el)) return
    mounted.add(el)
    const d = el.dataset
    // `rectPath` does not guard its amp/radius/w/h, so one NaN yields an all-NaN path string and an
    // invisible frame. The design's `num(v, d)` screened that with `isFinite`; a malformed
    // `data-ink-radius="abc"` has to fall back to the kind's default rather than erase the outline.
    const num = (value: string | undefined) => {
      if (value === undefined || value.trim() === '') return undefined
      const parsed = Number(value)
      return Number.isFinite(parsed) ? parsed : undefined
    }
    attachInk(el, {
      kind: KINDS.find((kind) => kind === d.ink) ?? 'sheet',
      inset: num(d.inkInset),
      radius: num(d.inkRadius),
      amp: num(d.inkAmp),
      sw: num(d.inkSw),
      color: d.inkColor,
      seed: num(d.inkSeed),
      op: num(d.inkOp),
      double: d.inkDouble !== undefined ? true : undefined,
      hatch: d.inkHatch === undefined ? undefined : d.inkHatch !== 'off',
    })
  })
}
