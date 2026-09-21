import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import DetailHero from '~/Components/Recipe/DetailHero.vue'
import { washFor } from '~/Utilities/BrandColor'

// Count star-family FontAwesome icons (fa-star, fa-star-half-stroke) in rendered markup.
// `fa-star\b` matches the leading token of each variant exactly once, so this equals the
// number of star glyphs actually painted — the kit's rating line paints one, never a row.
const countStars = (html: string) => (html.match(/fa-star\b/g) ?? []).length

const count = (html: string, needle: string) => html.split(needle).length - 1

/** The whole opening tag of the first element carrying `needle`, so nothing pins Vue's attribute order. */
const openTag = (html: string, needle: string) => {
  const at = html.indexOf(needle)
  expect(at, `${needle} is not in the render`).toBeGreaterThan(-1)
  return html.slice(html.lastIndexOf('<', at), html.indexOf('>', at) + 1)
}

/** The paragraph `needle` sits in, fragment anchors and all: slot content is not a tag of its own. */
const paragraph = (html: string, needle: string) => {
  const at = html.indexOf(needle)
  expect(at, `${needle} is not in the render`).toBeGreaterThan(-1)
  return html.slice(html.lastIndexOf('<p ', at), html.indexOf('</p>', at) + 4)
}

/** Rendered copy with the markup taken out — tags drop, their spacing does not, so a phrase reads as it will. */
const text = (html: string) =>
  html
    .replace(/<!--.*?-->/g, '')
    .replace(/<[^>]*>/g, '')
    .replace(/\s+/g, ' ')
    .trim()

/** Text of every Sono-set value, in document order: the identity sets every number this way. */
const sonoValues = (html: string) =>
  [...html.matchAll(/class="[^"]*kcc-num[^"]*"[^>]*>(.*?)<\/span>/g)].map((m) => m[1]!.replace(/<[^>]*>/g, '').trim())

const TITLE = 'Brown Butter Gnocchi'
const DESCRIPTION = 'Crisp-edged pillows in a nutty brown butter.'

const render = (props: Record<string, unknown> = {}, slots?: Record<string, () => unknown>) =>
  renderSsr(DetailHero, { title: TITLE, seed: TITLE, description: DESCRIPTION, ...props }, slots)

describe('DetailHero as a slip', () => {
  it('is a recipe slip torn from the hero preset and padded to the hero rule', async () => {
    const html = await render()
    const slip = openTag(html, 'kcc-slip')
    expect(slip).toContain('kcc-recipe')
    expect(slip).toContain('kcc-tear-hero')
    expect(openTag(html, 'kcc-sheet').replace(/\s/g, '')).toContain('--pad:48px')
    expect(html).not.toContain('sk-')
  })

  // Bottom-right: the kick lines (soft ink) run along the left, and soft ink over a wash core fails AA in
  // the dark ramp, so the pool must not sit under them.
  it('pools the recipe wash in the bottom-right corner, clear of the kick lines', async () => {
    const html = await render()
    const wash = openTag(html, 'kcc-wash').replace(/\s/g, '')
    expect(wash).toContain(`--c:var(--color-${washFor(TITLE)})`)
    expect(wash).toContain('--x:100%')
    expect(wash).toContain('--y:100%')
    expect(wash).toContain('--w:34%')
    // Capped so the ring ends inside the bottom padding whatever the sheet's height.
    expect(wash).toContain('--h:min(60%,68px)')
  })

  it('pins the large tile and its tape outside the tear, with a strip on the sheet', async () => {
    const html = await render({ icon: 'fa-duotone fa-wheat' })
    const tilewrap = openTag(html, 'kcc-tilewrap').replace(/\s/g, '')
    expect(tilewrap).toContain('top:-22px')
    expect(tilewrap).toContain('left:40px')
    expect(html).toContain('kcc-tile--lg')
    expect(count(html.replace(/\s/g, ''), `--c:var(--color-${washFor(TITLE)})`)).toBe(2)
    expect(count(html, 'kcc-tape')).toBe(2)
    expect(html.indexOf('kcc-tilewrap')).toBeGreaterThan(html.indexOf(DESCRIPTION))
  })

  it('sizes the tile from the kit, not from the caller', async () => {
    const html = await render({ icon: 'fa-duotone fa-wheat' })
    expect(html).not.toContain('h-40')
    expect(html).not.toContain('text-7xl')
    expect(html).not.toContain('size-full')
  })

  it('keeps the page heading, at display size', async () => {
    const html = await render()
    const heading = openTag(html, 'kcc-h3')
    expect(heading.startsWith('<h1')).toBe(true)
    expect(html).toContain(`>${TITLE}</h1>`)
  })

  it('sets the title beside the description in two columns from 640px', async () => {
    const html = await render()
    expect(openTag(html, 'sm:grid-cols-2')).toContain('grid')
    expect(html.indexOf('kcc-h3')).toBeLessThan(html.indexOf(DESCRIPTION))
    expect(paragraph(html, DESCRIPTION)).toContain('kcc-body')
  })

  it('opens the left column with the eyebrow as a kick and closes the right with the footer', async () => {
    const html = await render({}, { eyebrow: () => 'Pasta', footer: () => 'Vegetarian' })
    expect(paragraph(html, 'Pasta')).toContain('kcc-kick')
    expect(html.indexOf('Pasta')).toBeLessThan(html.indexOf(TITLE))
    expect(html.indexOf('Vegetarian')).toBeGreaterThan(html.indexOf(DESCRIPTION))
  })
})

describe('DetailHero rating line', () => {
  it('prints one duotone star with the average and the review count in Sono', async () => {
    const html = await render({ averageRating: 4.5, reviewCount: 5 })
    expect(openTag(html, 'fa-star')).toBe('<i class="fa-duotone fa-star" aria-hidden="true">')
    expect(countStars(html)).toBe(1)
    expect(sonoValues(html)).toEqual(['4.5', '5'])
    expect(text(html)).toContain('· 5 Reviews')
    // The separator is decoration: hidden from assistive tech, still read as it prints.
    expect(html).toContain('<span aria-hidden="true">· </span><span class="kcc-num">5</span>')
  })

  it('keeps the readonly star rating’s accessible name on the average', async () => {
    const html = await render({ averageRating: 4.5, reviewCount: 5 })
    const average = openTag(html, '4.5')
    expect(average).toContain('role="img"')
    expect(average).toContain('aria-label="4.5 of 5 stars"')
  })

  // An unreviewed recipe reads as text only, matching the empty state on the recipe cards.
  // A zero count and a missing one are the same state, so both inputs must stay starless.
  it.each([
    ['no reviews', { averageRating: 0, reviewCount: 0 }],
    ['no rating data at all', {}],
  ])('renders no star with %s', async (_label, props) => {
    const html = await render(props)
    expect(countStars(html)).toBe(0)
    expect(html).toContain('NoRatingsYet')
  })

  it('keeps the times-cooked hook, its number in Sono and its icon uncoloured', async () => {
    const html = await render({ timesCooked: 12 })
    expect(openTag(html, 'times-cooked')).toContain('data-testid="times-cooked"')
    expect(openTag(html, 'fa-fire-burner')).toBe('<i class="fa-duotone fa-fire-burner" aria-hidden="true">')
    expect(sonoValues(html)).toEqual(['12'])
    expect(text(html)).toContain('12 TimesCooked')
  })

  it('drops the times-cooked hook when nobody has cooked it', async () => {
    expect(await render({ timesCooked: 0 })).not.toContain('times-cooked')
  })

  it('names the author at the end of the line', async () => {
    const html = await render({ authorName: 'Ida Soerensen' })
    expect(text(html)).toContain('By Ida Soerensen')
    expect(paragraph(html, 'Ida Soerensen')).toContain('kcc-kick')
  })

  it('sets the whole line as a kick in ink', async () => {
    const html = await render({ averageRating: 4.5, reviewCount: 5 })
    const line = openTag(html, 'kcc-kick')
    expect(line.startsWith('<p')).toBe(true)
    expect(line).toMatch(/text-ink(?![-\w])/)
  })
})
