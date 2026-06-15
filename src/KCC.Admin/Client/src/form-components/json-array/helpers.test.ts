import { describe, expect, it } from "vitest";
import {
  formatIngredientSummary,
  formatQuantity,
  isIngredientValid,
  isInstructionValid,
  moveItem,
  normalizeIngredient,
  parseItems,
  reconcileIncomingValue,
  serializeItems,
  stampSteps,
} from "./helpers";
import { Ingredient, Instruction } from "./types";

describe("parseItems", () => {
  it("returns an empty list for null, empty, or whitespace", () => {
    expect(parseItems(null)).toEqual({ items: [], error: false });
    expect(parseItems("")).toEqual({ items: [], error: false });
    expect(parseItems("   ")).toEqual({ items: [], error: false });
  });

  it("parses a valid JSON array", () => {
    const json = `[{"name":"Bacon","quantity":3,"unit":"Slices","isEyeballed":false}]`;
    const result = parseItems<Ingredient>(json);
    expect(result.error).toBe(false);
    expect(result.items).toHaveLength(1);
    expect(result.items[0].name).toBe("Bacon");
  });

  it("flags invalid JSON without throwing", () => {
    expect(parseItems("{not json")).toEqual({ items: [], error: true });
  });

  it("flags valid JSON that is not an array", () => {
    expect(parseItems(`{"a":1}`)).toEqual({ items: [], error: true });
  });
});

describe("serializeItems", () => {
  it("serializes an empty list to an empty string (so required validation trips)", () => {
    expect(serializeItems([])).toBe("");
  });

  it("round-trips a non-empty list as compact JSON", () => {
    const items: Instruction[] = [{ step: 1, text: "Boil water." }];
    expect(serializeItems(items)).toBe(`[{"step":1,"text":"Boil water."}]`);
  });
});

describe("moveItem", () => {
  it("moves an item from one index to another", () => {
    expect(moveItem(["a", "b", "c"], 0, 2)).toEqual(["b", "c", "a"]);
  });

  it("returns the same order for no-op or out-of-bounds moves", () => {
    expect(moveItem(["a", "b"], 1, 1)).toEqual(["a", "b"]);
    expect(moveItem(["a", "b"], 5, 0)).toEqual(["a", "b"]);
    expect(moveItem(["a", "b"], 0, 5)).toEqual(["a", "b"]);
    expect(moveItem(["a", "b"], -1, 0)).toEqual(["a", "b"]);
  });
});

describe("normalizeIngredient", () => {
  it("clears quantity and unit when eyeballed", () => {
    const item: Ingredient = { name: "Salt", quantity: 2, unit: "Tsp", isEyeballed: true };
    expect(normalizeIngredient(item)).toEqual({ name: "Salt", quantity: null, unit: "", isEyeballed: true });
  });

  it("leaves a measured ingredient untouched", () => {
    const item: Ingredient = { name: "Bacon", quantity: 3, unit: "Slices", isEyeballed: false };
    expect(normalizeIngredient(item)).toEqual(item);
  });
});

describe("stampSteps", () => {
  it("renumbers steps by position", () => {
    const items: Instruction[] = [{ step: 9, text: "b" }, { step: 4, text: "a" }];
    expect(stampSteps(items)).toEqual([{ step: 1, text: "b" }, { step: 2, text: "a" }]);
  });
});

describe("validation", () => {
  it("requires a non-empty ingredient name", () => {
    expect(isIngredientValid({ name: "Bacon", quantity: 1, unit: "", isEyeballed: false })).toBe(true);
    expect(isIngredientValid({ name: "  ", quantity: 1, unit: "", isEyeballed: false })).toBe(false);
  });

  it("requires non-empty instruction text", () => {
    expect(isInstructionValid({ step: 1, text: "Stir." })).toBe(true);
    expect(isInstructionValid({ step: 1, text: "" })).toBe(false);
  });
});

describe("formatQuantity", () => {
  it("renders whole numbers as-is", () => {
    expect(formatQuantity(3)).toBe("3");
  });

  it("renders common fractions as glyphs", () => {
    expect(formatQuantity(0.5)).toBe("½");
    expect(formatQuantity(0.25)).toBe("¼");
    expect(formatQuantity(1.5)).toBe("1½");
  });

  it("falls back to the decimal for uncommon fractions", () => {
    expect(formatQuantity(0.4)).toBe("0.4");
  });
});

describe("reconcileIncomingValue", () => {
  const json = `[{"step":1,"text":"Boil water."}]`;

  it("keeps local state (returns null) when the incoming value is our own echo", () => {
    // After we call onChange, Kentico re-renders with the exact string we emitted — do not reset.
    expect(reconcileIncomingValue(json, json)).toBeNull();
    expect(reconcileIncomingValue("", "")).toBeNull();
    expect(reconcileIncomingValue(undefined, undefined)).toBeNull();
  });

  it("re-hydrates from an external change (e.g. revert to published)", () => {
    // The field was edited to a different draft value, then reverted to the published JSON.
    const result = reconcileIncomingValue<Instruction>(json, `[{"step":1,"text":"edited"}]`);
    expect(result).not.toBeNull();
    expect(result!.error).toBe(false);
    expect(result!.items).toEqual([{ step: 1, text: "Boil water." }]);
  });

  it("re-hydrates to an empty list when reverting to a cleared/empty published value", () => {
    expect(reconcileIncomingValue(null, json)).toEqual({ items: [], error: false });
    expect(reconcileIncomingValue("", json)).toEqual({ items: [], error: false });
  });

  it("flags an external value that is not valid JSON", () => {
    expect(reconcileIncomingValue("{not json", json)).toEqual({ items: [], error: true });
  });

  it("re-hydrates a late-arriving initial value (mounted before the value was supplied)", () => {
    expect(reconcileIncomingValue<Instruction>(json, undefined)).toEqual({
      items: [{ step: 1, text: "Boil water." }],
      error: false,
    });
  });
});

describe("formatIngredientSummary", () => {
  it("formats a measured ingredient", () => {
    expect(formatIngredientSummary({ name: "Bacon", quantity: 3, unit: "Slices", isEyeballed: false })).toBe("3 Slices Bacon");
  });

  it("formats an eyeballed ingredient as to taste", () => {
    expect(formatIngredientSummary({ name: "Salt", quantity: null, unit: "", isEyeballed: true })).toBe("Salt — to taste");
  });

  it("omits missing quantity or unit", () => {
    expect(formatIngredientSummary({ name: "Eggs", quantity: 4, unit: "", isEyeballed: false })).toBe("4 Eggs");
  });
});
