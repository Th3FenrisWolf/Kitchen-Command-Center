import React from "react";
import { formatDateTime } from "../shared/format";
import { StarRating } from "../shared/StarRating";
import type { ContributionTotals } from "../shared/types";

interface SummaryStripProps {
  totals: ContributionTotals;
}

export const SummaryStrip: React.FC<SummaryStripProps> = ({ totals }) => (
  <div className="kcc-contributions-tab__summary">
    <div className="kcc-contributions-tab__stat">
      <span className="kcc-contributions-tab__stat-label">Rating</span>
      <StarRating average={totals.averageRating} count={totals.reviewCount} />
    </div>
    <Stat label="Reviews" value={totals.reviewCount} />
    <Stat label="Cook notes" value={totals.noteCount} />
    <Stat label="Cooked" value={totals.cookedCount} />
    {totals.lastActivity !== null && (
      <div className="kcc-contributions-tab__stat">
        <span className="kcc-contributions-tab__stat-label">Last activity</span>
        <span className="kcc-contributions-tab__stat-value kcc-contributions-tab__stat-value--small">
          {formatDateTime(totals.lastActivity)}
        </span>
      </div>
    )}
  </div>
);

const Stat: React.FC<{ label: string; value: number }> = ({ label, value }) => (
  <div className="kcc-contributions-tab__stat">
    <span className="kcc-contributions-tab__stat-label">{label}</span>
    <span className="kcc-contributions-tab__stat-value">{value}</span>
  </div>
);
