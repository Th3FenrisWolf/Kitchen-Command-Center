import type { ContributionEntryPage, ContributionTotals } from "../shared/types";

export interface VariantContributionRow {
  variantName: string;
  averageRating: number | null;
  reviewCount: number;
  noteCount: number;
  cookedCount: number;
  lastActivity: string | null;
  contributionsUrl: string;
}

export interface RecipeContributionsProperties {
  totals: ContributionTotals;
  variants: VariantContributionRow[];
  orphaned: ContributionTotals;
}

export interface VariantContributionsProperties {
  totals: ContributionTotals;
  ratingDistribution: number[];
  recipeContributionsUrl: string;
  entryPageSize: number;
  canDelete: boolean;
  reviews: ContributionEntryPage;
  notes: ContributionEntryPage;
}

export interface ContributionMutationResult {
  totals: ContributionTotals;
  ratingDistribution: number[];
  entries: ContributionEntryPage;
}

export interface ContributionEntriesArgs {
  page: number;
}

export interface ContributionEntryArgs {
  id: number;
}

export const Commands = {
  GetReviews: "GetReviews",
  GetCookNotes: "GetCookNotes",
  DeleteReview: "DeleteReview",
  DeleteCookNote: "DeleteCookNote",
} as const;
