import React from "react";
import { Icon, IconName } from "@kentico/xperience-admin-components";
import { formatAverage, starShapesFor, StarShape } from "./format";

const iconFor: Record<StarShape, IconName> = {
  full: "xp-star-full",
  semi: "xp-star-semi",
  empty: "xp-star-empty",
};

interface StarRatingProps {
  average: number | null;
  count?: number;
}

export const StarRating: React.FC<StarRatingProps> = ({ average, count }) => {
  if (average === null) {
    return <span className="kcc-contributions__muted">No reviews</span>;
  }

  const label = typeof count === "number" ? `${formatAverage(average)} (${count})` : formatAverage(average);

  return (
    <span className="kcc-contributions__stars">
      {starShapesFor(average).map((shape, index) => (
        <Icon key={index} name={iconFor[shape]} />
      ))}
      <span className="kcc-contributions__stars-label">{label}</span>
    </span>
  );
};
