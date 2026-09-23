import React from "react";

interface RatingDistributionProps {
  distribution: number[];
  total: number;
}

// Index 0 is the 1-star bucket, so the rows read downwards from five.
export const RatingDistribution: React.FC<RatingDistributionProps> = ({ distribution, total }) => (
  <div className="kcc-contributions-tab__distribution">
    {[5, 4, 3, 2, 1].map((star) => {
      const count = distribution[star - 1] ?? 0;

      return (
        <div key={star} className="kcc-contributions-tab__distribution-row">
          <span className="kcc-contributions__muted">{`${star}★`}</span>
          <span className="kcc-contributions-tab__bar">
            <span
              className="kcc-contributions-tab__bar-fill"
              style={{ width: total === 0 ? "0%" : `${(count / total) * 100}%` }}
            />
          </span>
          <span className="kcc-contributions__muted">{count}</span>
        </div>
      );
    })}
  </div>
);
