export type EntryKind = "review" | "note";

export interface EntryView {
  id: number;
  memberName: string;
  rating: number | null;
  text: string;
  created: string;
  editUrl: string;
}

export interface ContributionTotals {
  averageRating: number | null;
  reviewCount: number;
  noteCount: number;
  cookedCount: number;
  lastActivity: string | null;
}

export interface ContributionEntryPage {
  entries: EntryView[];
  totalCount: number;
  page: number;
}
