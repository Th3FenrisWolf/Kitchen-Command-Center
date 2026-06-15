import React from "react";
import { Ingredient } from "./types";

interface IngredientRowProps {
  item: Ingredient;
  onChange: (next: Ingredient) => void;
  disabled: boolean;
  invalid: boolean;
  unitsListId: string;
}

export const IngredientRow = ({ item, onChange, disabled, invalid, unitsListId }: IngredientRowProps) => {
  const eyeballed = item.isEyeballed;

  return (
    <div className={"kcc-ingredient-row" + (invalid ? " is-invalid" : "")}>
      <input
        className="kcc-ingredient-row__name"
        type="text"
        aria-label="Ingredient name"
        placeholder="Ingredient name"
        value={item.name}
        disabled={disabled}
        onChange={(e) => onChange({ ...item, name: e.target.value })}
      />
      <input
        className="kcc-ingredient-row__qty"
        type="number"
        step="any"
        min="0"
        aria-label="Quantity"
        placeholder="Qty"
        value={item.quantity ?? ""}
        disabled={disabled || eyeballed}
        onChange={(e) => onChange({ ...item, quantity: e.target.value === "" ? null : Number(e.target.value) })}
      />
      <input
        className="kcc-ingredient-row__unit"
        type="text"
        list={unitsListId}
        aria-label="Unit"
        placeholder="Unit"
        value={item.unit}
        disabled={disabled || eyeballed}
        onChange={(e) => onChange({ ...item, unit: e.target.value })}
      />
      <label className="kcc-ingredient-row__eyeballed">
        <input
          type="checkbox"
          checked={eyeballed}
          disabled={disabled}
          onChange={(e) =>
            onChange(
              e.target.checked
                ? { ...item, isEyeballed: true, quantity: null, unit: "" }
                : { ...item, isEyeballed: false },
            )
          }
        />
        <span>Eyeballed</span>
      </label>
    </div>
  );
};
