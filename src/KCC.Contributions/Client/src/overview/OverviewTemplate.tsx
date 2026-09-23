import React, { useEffect, useRef, useState } from "react";
import { usePageCommand, usePageCommandProvider } from "@kentico/xperience-admin-base";
import {
  Button,
  ButtonColor,
  ButtonSize,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Headline,
  HeadlineSize,
  Input,
  Pagination,
  Spinner,
} from "@kentico/xperience-admin-components";
import OVERVIEW_CSS from "./overview.css";
import SHARED_CSS from "../shared/contributions.css";
import { totalPagesFor } from "../shared/format";
import { toRecipeQueryArgs } from "./helpers";
import { RecipeGroup } from "./RecipeGroup";
import {
  Commands,
  OverviewProperties,
  RecipeOverviewPageResult,
  RecipeOverviewQueryArgs,
} from "./types";

const ratingFilters: { label: string; value: number | null }[] = [
  { label: "Any rating", value: null },
  { label: "≤ 2", value: 2 },
  { label: "≤ 3", value: 3 },
  { label: "≤ 4", value: 4 },
];

export const OverviewTemplate: React.FC<OverviewProperties> = () => {
  const [searchText, setSearchText] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [maxRating, setMaxRating] = useState<number | null>(null);
  const [page, setPage] = useState(0);
  const [result, setResult] = useState<RecipeOverviewPageResult>();
  const [loading, setLoading] = useState(true);
  const [refreshKey, setRefreshKey] = useState(0);
  const initialized = useRef(false);
  const { executeCommand } = usePageCommandProvider();

  usePageCommand<RecipeOverviewPageResult, RecipeOverviewQueryArgs>(Commands.GetRecipes, {
    data: toRecipeQueryArgs("", null, 0),
    executeOnMount: true,
    after: (data) => {
      initialized.current = true;
      if (data) {
        setResult(data);
      }
      setLoading(false);
    },
  });

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(searchText), 300);
    return () => clearTimeout(timeout);
  }, [searchText]);

  useEffect(() => {
    if (!initialized.current) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    executeCommand<RecipeOverviewPageResult, RecipeOverviewQueryArgs>(
      Commands.GetRecipes,
      toRecipeQueryArgs(debouncedSearch, maxRating, page))
      .then((data) => {
        if (cancelled) {
          return;
        }
        if (data) {
          setResult(data);
        }
        setLoading(false);
      })
      .catch(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [debouncedSearch, maxRating, page, refreshKey]);

  const onMutated = () => setRefreshKey((value) => value + 1);
  const totalPages = result === undefined ? 1 : totalPagesFor(result.totalCount, result.pageSize);

  return (
    <>
      <style>{SHARED_CSS}</style>
      <style>{OVERVIEW_CSS}</style>
      <div className="kcc-contributions-overview">
        <Headline size={HeadlineSize.M}>Community Contributions</Headline>
        <div className="kcc-contributions-overview__controls">
          <div className="kcc-contributions-overview__search">
            <Input
              label="Search recipes"
              value={searchText}
              onChange={(event: React.ChangeEvent<HTMLInputElement>) => {
                setSearchText(event.target.value);
                setPage(0);
              }}
            />
          </div>
          <div className="kcc-contributions-overview__filters">
            {ratingFilters.map((filter) => (
              <Button
                key={filter.label}
                label={filter.label}
                size={ButtonSize.S}
                color={maxRating === filter.value ? ButtonColor.Primary : ButtonColor.Secondary}
                onClick={() => {
                  setMaxRating(filter.value);
                  setPage(0);
                }}
              />
            ))}
          </div>
        </div>
        {loading && (
          <div className="kcc-contributions__loading"><Spinner /></div>
        )}
        {!loading && result !== undefined && result.recipes.length === 0 && (
          <Callout
            type={CalloutType.QuickTip}
            placement={CalloutPlacementType.OnDesk}
            headline="No contributions found"
          >
            No recipes match the current search and filters.
          </Callout>
        )}
        {!loading && result !== undefined && result.recipes.length > 0 && (
          <div className="kcc-contributions-overview__groups">
            {result.recipes.map((recipe) => (
              <RecipeGroup
                key={recipe.recipeGuid}
                recipe={recipe}
                refreshKey={refreshKey}
                onMutated={onMutated}
              />
            ))}
          </div>
        )}
        {result !== undefined && totalPages > 1 && (
          <div className="kcc-contributions-overview__footer">
            <Pagination
              selectedPage={page + 1}
              totalPages={totalPages}
              onPageChange={(newPageNumber) => setPage(newPageNumber - 1)}
            />
          </div>
        )}
      </div>
    </>
  );
};
