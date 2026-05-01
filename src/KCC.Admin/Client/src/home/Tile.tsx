import React from "react";
import { Icon, type IconName } from "@kentico/xperience-admin-components";
import type { Tile as TileData } from "./types";

interface Props {
  tile: TileData;
}

export const Tile: React.FC<Props> = ({ tile }) => (
  <a href={tile.url} className="kcc-home-tile" title={tile.name}>
    {tile.iconName && (
      <span className="kcc-home-tile__icon">
        <Icon name={tile.iconName as IconName} />
      </span>
    )}
    <div className="kcc-home-tile__name">{tile.name}</div>
  </a>
);
