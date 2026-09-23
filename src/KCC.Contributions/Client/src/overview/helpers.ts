import type { RecipeOverviewQueryArgs } from "./types";

export const toRecipeQueryArgs = (
  searchText: string,
  maxAverageRating: number | null,
  page: number,
): RecipeOverviewQueryArgs => {
  const trimmed = searchText.trim();
  return {
    page: Math.max(0, page),
    searchText: trimmed === "" ? null : trimmed,
    maxAverageRating,
  };
};
