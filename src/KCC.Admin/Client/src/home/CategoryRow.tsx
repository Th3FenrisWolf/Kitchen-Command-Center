import React from "react";
import type { CategoryGroup } from "./types";
import { Tile } from "./Tile";

interface Props {
  category: CategoryGroup;
}

export const CategoryRow: React.FC<Props> = ({ category }) => (
  <section className="kcc-home-category">
    <h2 className="kcc-home-category__heading">{category.category}</h2>
    <div className="kcc-home-category__tiles">
      {category.tiles.map((tile) => (
        <Tile key={tile.identifier} tile={tile} />
      ))}
    </div>
  </section>
);
