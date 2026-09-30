import type { Ingredient, Instruction } from './types.js'

export interface ParseResult<T> {
  items: T[]
  error: boolean
}

export function parseItems<T>(value: string | null | undefined): ParseResult<T> {
  if (value == null || value.trim() === '') {
    return { items: [], error: false }
  }

  try {
    const parsed: unknown = JSON.parse(value)
    return Array.isArray(parsed) ? { items: parsed as T[], error: false } : { items: [], error: true }
  } catch {
    return { items: [], error: true }
  }
}

// An empty list is no value at all, so a mandatory property fails validation instead of saving "[]".
export function serializeItems<T>(items: T[]): string | undefined {
  return items.length === 0 ? undefined : JSON.stringify(items)
}

// The editor keeps its own rows so half-typed input survives the value it emits coming straight back in. Anything
// else that arrives (a late initial value, a restored version) replaces the rows.
export function reconcileIncomingValue<T>(
  value: string | null | undefined,
  lastEmitted: string | null | undefined,
): ParseResult<T> | null {
  return value === lastEmitted ? null : parseItems<T>(value)
}

export function normalizeIngredient(item: Ingredient): Ingredient {
  return item.isEyeballed ? { ...item, quantity: null, unit: '' } : item
}

export function stampSteps(items: Instruction[]): Instruction[] {
  return items.map((item, index) => ({ ...item, step: index + 1 }))
}

export function isIngredientValid(item: Ingredient): boolean {
  return item.name.trim().length > 0
}

export function isInstructionValid(item: Instruction): boolean {
  return item.text.trim().length > 0
}

const commonFractions: Record<string, string> = {
  '0.25': '¼',
  '0.33': '⅓',
  '0.5': '½',
  '0.67': '⅔',
  '0.75': '¾',
}

export function formatQuantity(quantity: number): string {
  if (Number.isInteger(quantity)) {
    return String(quantity)
  }

  const whole = Math.floor(quantity)
  const glyph = commonFractions[String(Math.round((quantity - whole) * 100) / 100)]
  if (!glyph) {
    return String(quantity)
  }

  return whole === 0 ? glyph : `${whole}${glyph}`
}

export function formatIngredientSummary(item: Ingredient): string {
  if (item.isEyeballed) {
    return `${item.name} — to taste`
  }

  const quantity = item.quantity == null ? '' : formatQuantity(item.quantity)
  return [quantity, item.unit, item.name]
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .join(' ')
}
