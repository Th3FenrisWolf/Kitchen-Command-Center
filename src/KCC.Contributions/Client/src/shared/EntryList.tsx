import React from "react";
import { Button, ButtonColor, ButtonSize, LinkButton } from "@kentico/xperience-admin-components";
import { formatDateTime } from "./format";
import { StarRating } from "./StarRating";
import type { EntryView } from "./types";

interface EntryListProps {
  title: string;
  entries: EntryView[];
  totalCount: number;
  onShowMore: () => void;
  onDelete?: (entry: EntryView) => void;
  /** Supply to edit within the current screen; without it the entry's own edit URL is linked. */
  onEdit?: (entry: EntryView) => void;
}

export const EntryList: React.FC<EntryListProps> = ({ title, entries, totalCount, onShowMore, onDelete, onEdit }) => {
  if (totalCount === 0) {
    return null;
  }

  return (
    <div className="kcc-contributions__entries">
      <div className="kcc-contributions__entries-title">{`${title} (${totalCount})`}</div>
      {entries.map((entry) => (
        <div key={entry.id} className="kcc-contributions__entry">
          <div className="kcc-contributions__entry-body">
            <div className="kcc-contributions__entry-meta">
              <span className="kcc-contributions__entry-member">{entry.memberName}</span>
              {entry.rating !== null && <StarRating average={entry.rating} />}
              <span className="kcc-contributions__muted">{formatDateTime(entry.created)}</span>
            </div>
            {entry.text !== "" && (
              <div className="kcc-contributions__entry-text">{entry.text}</div>
            )}
          </div>
          <div className="kcc-contributions__entry-actions">
            {onEdit === undefined ? (
              <LinkButton
                label="Edit"
                icon="xp-arrow-right-top-square"
                size={ButtonSize.XS}
                color={ButtonColor.Tertiary}
                href={entry.editUrl}
              />
            ) : (
              <Button
                label="Edit"
                icon="xp-edit"
                size={ButtonSize.XS}
                color={ButtonColor.Tertiary}
                onClick={() => onEdit(entry)}
              />
            )}
            {onDelete !== undefined && (
              <Button
                label="Delete"
                icon="xp-bin"
                size={ButtonSize.XS}
                color={ButtonColor.Tertiary}
                destructive
                onClick={() => onDelete(entry)}
              />
            )}
          </div>
        </div>
      ))}
      {entries.length < totalCount && (
        <Button
          label="Show more"
          size={ButtonSize.S}
          color={ButtonColor.Secondary}
          onClick={onShowMore}
        />
      )}
    </div>
  );
};
