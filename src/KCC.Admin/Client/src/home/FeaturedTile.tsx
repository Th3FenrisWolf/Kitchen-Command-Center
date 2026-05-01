import React from "react";
import { Icon, type IconName } from "@kentico/xperience-admin-components";
import type { FeaturedTile as FeaturedTileData } from "./types";

interface Props {
  tile: FeaturedTileData;
}

export const FeaturedTile: React.FC<Props> = ({ tile }) => (
  <a href={tile.url} className="kcc-home-featured-tile" title={tile.name}>
    <div className="kcc-home-featured-tile__header">
      {tile.iconName && (
        <span className="kcc-home-featured-tile__icon">
          <Icon name={tile.iconName as IconName} />
        </span>
      )}
      <div className="kcc-home-featured-tile__name">{tile.name}</div>
    </div>
    {tile.description && (
      <div className="kcc-home-featured-tile__description">
        {tile.description}
      </div>
    )}
    {tile.stats.length > 0 && (
      <div className="kcc-home-featured-tile__stats">
        {tile.stats.map((stat, i) => (
          <div
            key={`${stat.label}-${i}`}
            className="kcc-home-featured-tile__stat"
          >
            <div className="kcc-home-featured-tile__stat-value">
              {stat.value}
            </div>
            <div className="kcc-home-featured-tile__stat-label">
              {stat.label}
            </div>
          </div>
        ))}
      </div>
    )}
  </a>
);
