import React, { useState } from "react";
import { Route, Routes, useNavigate } from "react-router-dom";
import { RoutingContentPlaceholder, usePageCommandProvider } from "@kentico/xperience-admin-base";
import {
  ButtonColor,
  ButtonSize,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Headline,
  HeadlineSize,
  LinkButton,
  SnackbarItemVariant,
  useSnackbar,
} from "@kentico/xperience-admin-components";
import SHARED_CSS from "../shared/contributions.css";
import TAB_CSS from "./tabs.css";
import { DeleteEntryDialog } from "../shared/DeleteEntryDialog";
import { EntryList } from "../shared/EntryList";
import { appendPage, toRouterPath } from "../shared/format";
import type { ContributionEntryPage, EntryKind, EntryView } from "../shared/types";
import { RatingDistribution } from "./RatingDistribution";
import { SummaryStrip } from "./SummaryStrip";
import {
  Commands,
  ContributionEntriesArgs,
  ContributionEntryArgs,
  ContributionMutationResult,
  VariantContributionsProperties,
} from "./types";

/**
 * The edit forms for a review and a cook note are routed beneath this tab, so the template has to
 * yield the content area to them rather than always drawing the lists. This is the part a page
 * inherits for free from a platform template and has to state for itself in a custom one.
 *
 * The surface and its stylesheet belong out here rather than on the lists, because the routed forms
 * are drawn entirely by platform components: a client module is only fetched when one of its own
 * components renders, so styles declared inside the index route never reach the form beneath it.
 */
export const VariantContributionsTemplate: React.FC<VariantContributionsProperties> = (props) => (
  <>
    <style>{SHARED_CSS}</style>
    <style>{TAB_CSS}</style>
    <div className="kcc-contributions-tab">
      <Routes>
        <Route index element={<VariantContributions {...props} />} />
        <Route path="*" element={<RoutingContentPlaceholder />} />
      </Routes>
    </div>
  </>
);

const VariantContributions: React.FC<VariantContributionsProperties> = ({
  totals: initialTotals,
  ratingDistribution: initialDistribution,
  recipeContributionsUrl,
  canDelete,
  reviews: initialReviews,
  notes: initialNotes,
}) => {
  const [totals, setTotals] = useState(initialTotals);
  const [ratingDistribution, setRatingDistribution] = useState(initialDistribution);
  const [reviews, setReviews] = useState(initialReviews);
  const [notes, setNotes] = useState(initialNotes);
  const [pendingDelete, setPendingDelete] = useState<{ kind: EntryKind; entry: EntryView } | null>(null);
  const [deleting, setDeleting] = useState(false);
  const { executeCommand } = usePageCommandProvider();
  const { addMessage } = useSnackbar();
  const navigate = useNavigate();

  const showMore = (kind: EntryKind) => {
    const current = kind === "review" ? reviews : notes;
    const command = kind === "review" ? Commands.GetReviews : Commands.GetCookNotes;
    const apply = kind === "review" ? setReviews : setNotes;

    void executeCommand<ContributionEntryPage, ContributionEntriesArgs>(command, { page: current.page + 1 })
      .then((result) => {
        if (result) {
          apply({ ...result, entries: appendPage(current.entries, result.entries, result.page) });
        }
      })
      .catch(() => addMessage({ message: "Could not load more entries.", variant: SnackbarItemVariant.Error }));
  };

  const confirmDelete = () => {
    if (pendingDelete === null) {
      return;
    }

    const { kind, entry } = pendingDelete;
    const command = kind === "review" ? Commands.DeleteReview : Commands.DeleteCookNote;
    const apply = kind === "review" ? setReviews : setNotes;

    setDeleting(true);
    executeCommand<ContributionMutationResult, ContributionEntryArgs>(command, { id: entry.id })
      .then((result) => {
        // A refused command resolves with nothing, which is indistinguishable from an emptied list
        // unless the absence is checked for: assigning it would blank the list while deleting nothing.
        if (result) {
          setTotals(result.totals);
          setRatingDistribution(result.ratingDistribution);
          apply(result.entries);
        }
      })
      .catch(() => addMessage({ message: "Delete failed.", variant: SnackbarItemVariant.Error }))
      .then(() => {
        setDeleting(false);
        setPendingDelete(null);
      });
  };

  const onDelete = canDelete ? (kind: EntryKind) => (entry: EntryView) => setPendingDelete({ kind, entry }) : undefined;

  // The form is a child route of this tab, so it is reached by the router rather than by an anchor
  // that would reload the whole administration.
  const onEdit = (entry: EntryView) => navigate(toRouterPath(entry.editUrl));

  return (
    <>
      <div className="kcc-contributions-tab__header">
        <Headline size={HeadlineSize.M}>Contributions</Headline>
        {recipeContributionsUrl !== "" && (
          <LinkButton
            label="All variants"
            icon="xp-arrow-right-top-square"
            size={ButtonSize.S}
            color={ButtonColor.Secondary}
            href={recipeContributionsUrl}
          />
        )}
      </div>

      <SummaryStrip totals={totals} />

      {totals.reviewCount > 0 && (
        <RatingDistribution distribution={ratingDistribution} total={totals.reviewCount} />
      )}

      {totals.reviewCount === 0 && totals.noteCount === 0 ? (
        <Callout
          type={CalloutType.QuickTip}
          placement={CalloutPlacementType.OnDesk}
          headline="Nothing written yet"
        >
          {totals.cookedCount > 0
            ? `${totals.cookedCount} member${totals.cookedCount === 1 ? " has" : "s have"} marked this variant cooked, but nobody has left a review or a cook note.`
            : "No reviews, cook notes or cooked marks have been left against this variant."}
        </Callout>
      ) : (
        <>
          <EntryList
            title="Reviews"
            entries={reviews.entries}
            totalCount={reviews.totalCount}
            onShowMore={() => showMore("review")}
            onDelete={onDelete?.("review")}
            onEdit={onEdit}
          />
          <EntryList
            title="Cook notes"
            entries={notes.entries}
            totalCount={notes.totalCount}
            onShowMore={() => showMore("note")}
            onDelete={onDelete?.("note")}
            onEdit={onEdit}
          />
        </>
      )}

      {pendingDelete !== null && (
        <DeleteEntryDialog
          kind={pendingDelete.kind}
          entry={pendingDelete.entry}
          inProgress={deleting}
          onConfirm={confirmDelete}
          onCancel={() => setPendingDelete(null)}
        />
      )}
    </>
  );
};
