import React from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import { JsonArrayEditor } from "./JsonArrayEditor";
import { IngredientRow } from "./IngredientRow";
import { Ingredient } from "./types";
import { formatIngredientSummary, isIngredientValid, normalizeIngredient } from "./helpers";
import INGREDIENT_CSS from "./ingredient-row.css";

/** `Units` comes from the C# IngredientsEditorFormComponent client properties (camelCased by Kentico). */
interface IngredientsEditorProps extends FormComponentProps<string> {
  units: string[];
}

export const IngredientsEditorFormComponent = (props: IngredientsEditorProps) => {
  const units = props.units ?? [];
  const unitsListId = React.useId();

  return (
    <>
      <style>{INGREDIENT_CSS}</style>
      <datalist id={unitsListId}>
        {units.map((unit) => (
          <option key={unit} value={unit} />
        ))}
      </datalist>
      <JsonArrayEditor<Ingredient>
        {...props}
        newItem={() => ({ name: "", quantity: null, unit: "", isEyeballed: false })}
        renderRow={(item, onChange, ctx) => (
          <IngredientRow
            item={item}
            onChange={onChange}
            disabled={ctx.disabled}
            invalid={ctx.invalid}
            unitsListId={unitsListId}
          />
        )}
        renderSummary={formatIngredientSummary}
        isItemValid={isIngredientValid}
        prepareForSave={(items) => items.map(normalizeIngredient)}
        summaryListTag="ul"
        addLabel="Add ingredient"
        emptyText="No ingredients yet."
        invalidMessage="Every ingredient needs a name."
        emptyInvalidMessage="Add at least one ingredient."
      />
    </>
  );
};
