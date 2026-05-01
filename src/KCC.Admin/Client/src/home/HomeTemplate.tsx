import React from "react";
import type { HomeProperties } from "./types";
import { FeaturedTile } from "./FeaturedTile";
import { CategoryRow } from "./CategoryRow";
import HOME_CSS from "./HomeTemplate.css";

const styles: Record<string, React.CSSProperties> = {
  home: {
    padding: 24,
    height: "100%",
    overflowY: "auto",
    boxSizing: "border-box",
  },
};

export const HomeTemplate: React.FC<HomeProperties> = (props) => (
  <>
    <style>{HOME_CSS}</style>
    <div style={styles.home}>
      <h1 className="kcc-home__title">Home</h1>
      {props.featuredTiles.length > 0 && (
        <div className="kcc-home__featured-row">
          {props.featuredTiles.map((tile) => (
            <FeaturedTile key={tile.identifier} tile={tile} />
          ))}
        </div>
      )}
      {props.categories.map((category) => (
        <CategoryRow key={category.category} category={category} />
      ))}
    </div>
  </>
);
