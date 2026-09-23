export interface OverviewProperties {
  recipePageSize: number;
  entryPageSize: number;
}

export interface RecipeOverviewQueryArgs {
  page: number;
  searchText: string | null;
  maxAverageRating: number | null;
}

export interface RecipeOverviewRow {
  recipeGuid: string;
  recipeName: string;
  averageRating: number | null;
  reviewCount: number;
  noteCount: number;
  cookedCount: number;
  lastActivity: string | null;
}

export interface RecipeOverviewPageResult {
  recipes: RecipeOverviewRow[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface RecipeVariantsArgs {
  recipeGuid: string;
}

export interface VariantOverviewRow {
  variantGuid: string;
  variantName: string;
  averageRating: number | null;
  reviewCount: number;
  noteCount: number;
  cookedCount: number;
}

export interface RecipeVariantsResult {
  variants: VariantOverviewRow[];
}

export interface VariantEntriesArgs {
  variantGuid: string;
  page: number;
}

export interface ReviewEntry {
  id: number;
  memberName: string;
  rating: number;
  textSnippet: string;
  created: string;
  editUrl: string;
}

export interface ReviewEntriesResult {
  entries: ReviewEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CookNoteEntry {
  id: number;
  memberName: string;
  textSnippet: string;
  created: string;
  editUrl: string;
}

export interface CookNoteEntriesResult {
  entries: CookNoteEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DeleteEntryArgs {
  id: number;
}

export interface DeleteEntryResult {
  deleted: boolean;
}

export const Commands = {
  GetRecipes: "GetRecipes",
  GetRecipeVariants: "GetRecipeVariants",
  GetVariantReviews: "GetVariantReviews",
  GetVariantCookNotes: "GetVariantCookNotes",
  DeleteReview: "DeleteReview",
  DeleteCookNote: "DeleteCookNote",
} as const;
