import React from "react";
import { Dialog } from "@kentico/xperience-admin-components";
import type { EntryKind, EntryView } from "./types";

interface DeleteEntryDialogProps {
  kind: EntryKind;
  entry: EntryView;
  inProgress: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const DeleteEntryDialog: React.FC<DeleteEntryDialogProps> = ({ kind, entry, inProgress, onConfirm, onCancel }) => (
  <Dialog
    isOpen
    headline={kind === "review" ? "Delete review?" : "Delete cook note?"}
    onClose={onCancel}
    headerCloseButton={{ tooltipText: "Close" }}
    isDismissable
    actionInProgress={inProgress}
    confirmAction={{ label: "Delete", destructive: true, icon: "xp-bin", inProgress, onClick: onConfirm }}
    cancelAction={{ label: "Cancel", onClick: onCancel }}
  >
    <p className="kcc-contributions__dialog-text">
      This permanently removes the {kind === "review" ? "review" : "cook note"} by <strong>{entry.memberName}</strong>.
    </p>
  </Dialog>
);
