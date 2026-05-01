export interface HomeProperties {
  featuredTiles: FeaturedTile[];
  categories: CategoryGroup[];
}

export interface FeaturedTile {
  identifier: string;
  name: string;
  iconName: string;
  url: string;
  description: string;
  stats: TileStat[];
}

export interface TileStat {
  label: string;
  value: string;
}

export interface CategoryGroup {
  category: string;
  tiles: Tile[];
}

export interface Tile {
  identifier: string;
  name: string;
  iconName: string;
  url: string;
}
