import { Ingredient, Instruction } from "./types";

export interface ParseResult<T> {
  items: T[];
  error: boolean;
}

/** Parse the stored JSON string into items. Null/empty/whitespace → empty list; bad JSON → error flag (never throws). */
export function parseItems<T>(value: string | null | undefined): ParseResult<T> {
  if (value == null || value.trim() === "") {
    return { items: [], error: false };
  }
  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) {
      return { items: [], error: true };
    }
    return { items: parsed as T[], error: false };
  } catch {
    return { items: [], error: true };
  }
}

/** Serialize items to a compact JSON string. An empty list becomes "" so Kentico's required validation trips. */
export function serializeItems<T>(items: T[]): string {
  return items.length === 0 ? "" : JSON.stringify(items);
}

/**
 * Decide how to reconcile the editor's local state with an incoming `value` prop.
 *
 * The editor keeps a local copy of the parsed items so editing stays responsive. Kentico owns
 * the field value, though, and changes it out from under us on revert-to-published, undo,
 * version restore, language switch, or a late-arriving initial value. Re-hydrate from `value`
 * only when it differs from the string we last emitted via `onChange`; when it matches our own
 * echo, return `null` so in-progress edits are kept (no flicker, no fighting the user's typing).
 */
export function reconcileIncomingValue<T>(
  value: string | null | undefined,
  lastEmitted: string | null | undefined,
): ParseResult<T> | null {
  return value === lastEmitted ? null : parseItems<T>(value);
}

/** Pure reorder: move the item at `from` to `to`. Out-of-bounds or no-op returns the original array. */
export function moveItem<T>(items: T[], from: number, to: number): T[] {
  if (from === to || from < 0 || to < 0 || from >= items.length || to >= items.length) {
    return items.slice();
  }
  const next = items.slice();
  const [moved] = next.splice(from, 1);
  next.splice(to, 0, moved);
  return next;
}

/** When an ingredient is eyeballed it has no measurement: clear quantity and unit. */
export function normalizeIngredient(item: Ingredient): Ingredient {
  return item.isEyeballed ? { ...item, quantity: null, unit: "" } : item;
}

/** Renumber instruction steps to match their position (1-based). */
export function stampSteps(items: Instruction[]): Instruction[] {
  return items.map((item, index) => ({ ...item, step: index + 1 }));
}

/** Returns true when the ingredient has a non-empty name. */
export function isIngredientValid(item: Ingredient): boolean {
  return item.name.trim().length > 0;
}

/** Returns true when the instruction has non-empty text. */
export function isInstructionValid(item: Instruction): boolean {
  return item.text.trim().length > 0;
}

const COMMON_FRACTIONS: Record<string, string> = {
  "0.25": "¼",
  "0.33": "⅓",
  "0.5": "½",
  "0.67": "⅔",
  "0.75": "¾",
};

/** Render a quantity for the read-only summary, using common-fraction glyphs (e.g. 1.5 → "1½"). */
export function formatQuantity(quantity: number): string {
  if (Number.isInteger(quantity)) {
    return String(quantity);
  }
  const whole = Math.floor(quantity);
  const fraction = Math.round((quantity - whole) * 100) / 100;
  const glyph = COMMON_FRACTIONS[String(fraction)];
  if (!glyph) {
    return String(quantity);
  }
  return whole === 0 ? glyph : `${whole}${glyph}`;
}

/** Read-only one-line summary of an ingredient: "3 Slices Bacon" / "Salt — to taste". */
export function formatIngredientSummary(item: Ingredient): string {
  if (item.isEyeballed) {
    return `${item.name} — to taste`;
  }
  const quantity = item.quantity == null ? "" : formatQuantity(item.quantity);
  return [quantity, item.unit, item.name]
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .join(" ");
}
