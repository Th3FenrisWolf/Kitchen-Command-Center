import React, { useEffect, useState } from "react";
import { usePageCommandProvider } from "@kentico/xperience-admin-base";
import {
  BarItem,
  BarItemHeaderColumnAlign,
  SnackbarItemVariant,
  Spinner,
  useSnackbar,
} from "@kentico/xperience-admin-components";
import { appendPage } from "../shared/format";
import { DeleteEntryDialog } from "../shared/DeleteEntryDialog";
import { EntryList } from "../shared/EntryList";
import { StarRating } from "../shared/StarRating";
import type { EntryKind, EntryView } from "../shared/types";
import {
  Commands,
  CookNoteEntriesResult,
  CookNoteEntry,
  DeleteEntryArgs,
  DeleteEntryResult,
  ReviewEntriesResult,
  ReviewEntry,
  VariantEntriesArgs,
  VariantOverviewRow,
} from "./types";

interface VariantPanelProps {
  variant: VariantOverviewRow;
  refreshKey: number;
  onMutated: () => void;
}

interface EntryListState {
  entries: EntryView[];
  totalCount: number;
  page: number;
}

const emptyList: EntryListState = { entries: [], totalCount: 0, page: 0 };

export const VariantPanel: React.FC<VariantPanelProps> = ({ variant, refreshKey, onMutated }) => {
  const [expanded, setExpanded] = useState(false);
  const [loading, setLoading] = useState(false);
  const [reviews, setReviews] = useState(emptyList);
  const [notes, setNotes] = useState(emptyList);
  const [pendingDelete, setPendingDelete] = useState<{ kind: EntryKind; entry: EntryView } | null>(null);
  const [deleting, setDeleting] = useState(false);
  const { executeCommand } = usePageCommandProvider();
  const { addMessage } = useSnackbar();

  const loadReviews = (page: number) =>
    executeCommand<ReviewEntriesResult, VariantEntriesArgs>(Commands.GetVariantReviews, { variantGuid: variant.variantGuid, page })
      .then((result) => {
        if (result) {
          setReviews((previous) => ({
            entries: appendPage(previous.entries, result.entries.map(reviewToEntryView), page),
            totalCount: result.totalCount,
            page,
          }));
        }
      });

  const loadNotes = (page: number) =>
    executeCommand<CookNoteEntriesResult, VariantEntriesArgs>(Commands.GetVariantCookNotes, { variantGuid: variant.variantGuid, page })
      .then((result) => {
        if (result) {
          setNotes((previous) => ({
            entries: appendPage(previous.entries, result.entries.map(noteToEntryView), page),
            totalCount: result.totalCount,
            page,
          }));
        }
      });

  useEffect(() => {
    if (!expanded) {
      return;
    }

    setLoading(true);
    Promise.all([loadReviews(0), loadNotes(0)])
      .catch(() => undefined)
      .then(() => setLoading(false));
  }, [expanded, refreshKey]);

  const confirmDelete = () => {
    if (pendingDelete === null) {
      return;
    }

    const command = pendingDelete.kind === "review" ? Commands.DeleteReview : Commands.DeleteCookNote;
    setDeleting(true);
    executeCommand<DeleteEntryResult, DeleteEntryArgs>(command, { id: pendingDelete.entry.id })
      .then((result) => {
        if (result?.deleted) {
          addMessage({ message: pendingDelete.kind === "review" ? "Review deleted." : "Cook note deleted.", variant: SnackbarItemVariant.Success });
          onMutated();
        } else {
          addMessage({ message: "The entry no longer exists.", variant: SnackbarItemVariant.Error });
        }
      })
      .catch(() => addMessage({ message: "Delete failed.", variant: SnackbarItemVariant.Error }))
      .then(() => {
        setDeleting(false);
        setPendingDelete(null);
      });
  };

  return (
    <>
      <BarItem
        expanded={expanded}
        onHeaderClick={() => setExpanded((value) => !value)}
        headerColumns={[
          { content: <span className="kcc-contributions__name">{variant.variantName}</span> },
          { content: <StarRating average={variant.averageRating} count={variant.reviewCount} /> },
          { content: <span className="kcc-contributions__muted">{`${variant.noteCount} notes · ${variant.cookedCount} cooked`}</span>, align: BarItemHeaderColumnAlign.Right },
        ]}
      >
        {expanded && (loading
          ? <div className="kcc-contributions__loading"><Spinner /></div>
          : (
            <div>
              <EntryList
                title="Reviews"
                entries={reviews.entries}
                totalCount={reviews.totalCount}
                onShowMore={() => void loadReviews(reviews.page + 1)}
                onDelete={(entry) => setPendingDelete({ kind: "review", entry })}
              />
              <EntryList
                title="Cook notes"
                entries={notes.entries}
                totalCount={notes.totalCount}
                onShowMore={() => void loadNotes(notes.page + 1)}
                onDelete={(entry) => setPendingDelete({ kind: "note", entry })}
              />
              {reviews.totalCount === 0 && notes.totalCount === 0 && (
                <div className="kcc-contributions__entries">
                  <span className="kcc-contributions__muted">Only cooked marks exist for this variant.</span>
                </div>
              )}
            </div>
          ))}
      </BarItem>
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

const reviewToEntryView = (entry: ReviewEntry): EntryView => ({
  id: entry.id,
  memberName: entry.memberName,
  rating: entry.rating,
  text: entry.textSnippet,
  created: entry.created,
  editUrl: entry.editUrl,
});

const noteToEntryView = (entry: CookNoteEntry): EntryView => ({
  id: entry.id,
  memberName: entry.memberName,
  rating: null,
  text: entry.textSnippet,
  created: entry.created,
  editUrl: entry.editUrl,
});
