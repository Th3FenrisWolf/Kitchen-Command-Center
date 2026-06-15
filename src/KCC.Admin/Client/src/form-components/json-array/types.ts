/** One recipe ingredient. Mirrors KCC.Web/Features/Types/Recipe.ts (kept in sync by convention). */
export interface Ingredient {
  name: string;
  quantity: number | null;
  unit: string;
  isEyeballed: boolean;
}

/** One recipe instruction step. `step` is derived from position on save. */
export interface Instruction {
  step: number;
  text: string;
}
