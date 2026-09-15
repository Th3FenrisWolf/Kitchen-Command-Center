import { describe, expect, it } from "vitest";
import { toRecipeQueryArgs } from "./helpers";

describe("toRecipeQueryArgs", () => {
  it("normalizes blank search to null and clamps the page", () => {
    expect(toRecipeQueryArgs("   ", 2, -1)).toEqual({ page: 0, searchText: null, maxAverageRating: 2 });
  });

  it("trims search text", () => {
    expect(toRecipeQueryArgs("  mac  ", null, 3)).toEqual({ page: 3, searchText: "mac", maxAverageRating: null });
  });
});
