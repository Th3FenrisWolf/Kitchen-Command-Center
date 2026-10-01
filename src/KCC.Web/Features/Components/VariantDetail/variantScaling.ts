import type { Ingredient } from '~/Types/Recipe'

const FRACTIONS: readonly [number, string][] = [
  [0, ''],
  [0.125, '⅛'],
  [0.25, '¼'],
  [0.333, '⅓'],
  [0.5, '½'],
  [0.667, '⅔'],
  [0.75, '¾'],
  [0.875, '⅞'],
  [1, ''],
]

export function scaleQuantity(quantity: number, baseServings: number, currentServings: number): number {
  if (!baseServings || baseServings <= 0) return quantity
  return (quantity * currentServings) / baseServings
}

export function formatQuantity(x: number): string {
  if (x == null || Number.isNaN(x)) return ''
  let whole = Math.floor(x + 1e-6)
  const frac = x - whole
  let best = FRACTIONS[0]!
  let bestDistance = Infinity
  for (const candidate of FRACTIONS) {
    const distance = Math.abs(frac - candidate[0])
    if (distance < bestDistance) {
      bestDistance = distance
      best = candidate
    }
  }
  let glyph = best[1]
  if (best[0] === 1) {
    whole += 1
    glyph = ''
  }
  if (whole > 0 && glyph) return `${whole}${glyph}`
  if (whole > 0) return String(whole)
  if (glyph) return glyph
  return x.toFixed(2).replace(/\.0+$/, '')
}

export function formatIngredientAmount(ingredient: Ingredient, baseServings: number, currentServings: number): string {
  if (ingredient.isEyeballed || ingredient.quantity == null) return ''
  const scaled = scaleQuantity(ingredient.quantity, baseServings, currentServings)
  const qty = formatQuantity(scaled)
  return ingredient.unit ? `${qty} ${ingredient.unit}` : qty
}
