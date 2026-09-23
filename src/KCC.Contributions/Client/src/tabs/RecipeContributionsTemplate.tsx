import React, { useMemo, useState } from "react";
import {
  Callout,
  CalloutPlacementType,
  CalloutType,
  CellType,
  ColumnContentType,
  Headline,
  HeadlineSize,
  SortType,
  Table,
  type SortModel,
  type TableColumn,
  type TableRow,
} from "@kentico/xperience-admin-components";
import SHARED_CSS from "../shared/contributions.css";
import TAB_CSS from "./tabs.css";
import { formatDateTime, toRouterPath } from "../shared/format";
import { StarRating } from "../shared/StarRating";
import { SummaryStrip } from "./SummaryStrip";
import type { RecipeContributionsProperties, VariantContributionRow } from "./types";

const columns: TableColumn[] = [
  column("variantName", "Variant", { sortable: true, minWidth: 16 }),
  column("averageRating", "Rating", { sortable: true, contentType: ColumnContentType.Component }),
  column("reviewCount", "Reviews", { sortable: true, maxWidth: 10 }),
  column("noteCount", "Cook notes", { sortable: true, maxWidth: 10 }),
  column("cookedCount", "Cooked", { sortable: true, maxWidth: 10 }),
  column("lastActivity", "Last activity", { sortable: true }),
];

export const RecipeContributionsTemplate: React.FC<RecipeContributionsProperties> = ({
  totals,
  variants,
  orphaned,
}) => {
  const [sort, setSort] = useState<SortModel>({ sortBy: "variantName", sortType: SortType.Asc });

  const rows: TableRow[] = useMemo(
    () =>
      [...variants].sort(comparerFor(sort)).map((variant, index) => ({
        identifier: index,
        disabled: false,
        href: toRouterPath(variant.contributionsUrl),
        cells: [
          { type: CellType.String, value: variant.variantName },
          {
            // A component cell is handed the component itself and renders it with no props, so the
            // values have to be closed over rather than passed. An element here renders nothing and
            // takes the whole tab down with it.
            type: CellType.Component,
            component: () => <StarRating average={variant.averageRating} count={variant.reviewCount} />,
          },
          { type: CellType.String, value: String(variant.reviewCount) },
          { type: CellType.String, value: String(variant.noteCount) },
          { type: CellType.String, value: String(variant.cookedCount) },
          { type: CellType.String, value: formatDateTime(variant.lastActivity) },
        ],
      })),
    [variants, sort],
  );

  return (
    <>
      <style>{SHARED_CSS}</style>
      <style>{TAB_CSS}</style>
      <div className="kcc-contributions-tab">
        <Headline size={HeadlineSize.M}>Contributions</Headline>
        <SummaryStrip totals={totals} />

        {variants.length === 0 ? (
          <Callout
            type={CalloutType.QuickTip}
            placement={CalloutPlacementType.OnDesk}
            headline="No variants"
          >
            Reviews, cook notes and cooked marks are left against a variant. This recipe has none yet.
          </Callout>
        ) : (
          <Table columns={columns} rows={rows} sortModel={sort} onSortChange={setSort} isHeaderVisible />
        )}

        {!isEmpty(orphaned) && (
          <p className="kcc-contributions-tab__note kcc-contributions__muted">
            {describeOrphans(orphaned)} filed against a variant this recipe no longer has. They count
            towards the totals above and are reachable from the Community Contributions application.
          </p>
        )}
      </div>
    </>
  );
};

function column(name: string, caption: string, options: Partial<TableColumn> = {}): TableColumn {
  return {
    name,
    caption,
    visible: true,
    minWidth: 8,
    maxWidth: 100,
    contentType: ColumnContentType.Text,
    sortable: false,
    searchable: false,
    ...options,
  };
}

// One collator for every comparison rather than an options object per call, which re-resolves it.
const collator = new Intl.Collator(undefined, { numeric: true });

const comparerFor =
  (sort: SortModel) =>
  (left: VariantContributionRow, right: VariantContributionRow): number => {
    const direction = sort.sortType === SortType.Asc ? 1 : -1;
    const a = sortValue(left, sort.sortBy);
    const b = sortValue(right, sort.sortBy);

    // Counts and ratings compare as numbers; collating them as text would order 10 below 9, and a
    // variant with no rating has to land at one end rather than wherever "" happens to fall.
    if (typeof a === "number" || typeof b === "number") {
      return direction * (Number(a ?? -1) - Number(b ?? -1));
    }

    return direction * collator.compare(String(a ?? ""), String(b ?? ""));
  };

const sortValue = (row: VariantContributionRow, name: string): string | number | null =>
  (row as unknown as Record<string, string | number | null>)[name];

const isEmpty = (totals: RecipeContributionsProperties["orphaned"]): boolean =>
  totals.reviewCount === 0 && totals.noteCount === 0 && totals.cookedCount === 0;

const describeOrphans = (totals: RecipeContributionsProperties["orphaned"]): string => {
  const parts = [
    plural(totals.reviewCount, "review"),
    plural(totals.noteCount, "cook note"),
    plural(totals.cookedCount, "cooked mark"),
  ].filter((part) => part !== "");

  return parts.length === 1 ? `${parts[0]} was` : `${parts.slice(0, -1).join(", ")} and ${parts.at(-1)} were`;
};

const plural = (count: number, noun: string): string =>
  count === 0 ? "" : `${count} ${noun}${count === 1 ? "" : "s"}`;
