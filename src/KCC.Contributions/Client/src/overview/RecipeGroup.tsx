import React, { useEffect, useState } from "react";
import { usePageCommandProvider } from "@kentico/xperience-admin-base";
import { BarItem, BarItemHeaderColumnAlign, Spinner } from "@kentico/xperience-admin-components";
import { formatDateTime } from "../shared/format";
import { StarRating } from "../shared/StarRating";
import { VariantPanel } from "./VariantPanel";
import {
  Commands,
  RecipeOverviewRow,
  RecipeVariantsArgs,
  RecipeVariantsResult,
  VariantOverviewRow,
} from "./types";

interface RecipeGroupProps {
  recipe: RecipeOverviewRow;
  refreshKey: number;
  onMutated: () => void;
}

export const RecipeGroup: React.FC<RecipeGroupProps> = ({ recipe, refreshKey, onMutated }) => {
  const [expanded, setExpanded] = useState(false);
  const [loading, setLoading] = useState(false);
  const [variants, setVariants] = useState<VariantOverviewRow[]>();
  const { executeCommand } = usePageCommandProvider();

  useEffect(() => {
    if (!expanded) {
      return;
    }

    setLoading(true);
    executeCommand<RecipeVariantsResult, RecipeVariantsArgs>(Commands.GetRecipeVariants, { recipeGuid: recipe.recipeGuid })
      .then((result) => {
        if (result) {
          setVariants(result.variants);
        }
      })
      .catch(() => undefined)
      .then(() => setLoading(false));
  }, [expanded, refreshKey]);

  return (
    <BarItem
      expanded={expanded}
      onHeaderClick={() => setExpanded((value) => !value)}
      headerColumns={[
        { content: <span className="kcc-contributions__name">{recipe.recipeName}</span> },
        { content: <StarRating average={recipe.averageRating} count={recipe.reviewCount} /> },
        { content: <span className="kcc-contributions__muted">{`${recipe.noteCount} notes · ${recipe.cookedCount} cooked`}</span> },
        { content: <span className="kcc-contributions__muted">{formatDateTime(recipe.lastActivity)}</span>, align: BarItemHeaderColumnAlign.Right },
      ]}
    >
      {expanded && (loading || variants === undefined
        ? <div className="kcc-contributions__loading"><Spinner /></div>
        : (
          <div className="kcc-contributions-overview__variants">
            {variants.map((variant) => (
              <VariantPanel
                key={variant.variantGuid}
                variant={variant}
                refreshKey={refreshKey}
                onMutated={onMutated}
              />
            ))}
            {variants.length === 0 && (
              <span className="kcc-contributions__muted">No variants with contributions.</span>
            )}
          </div>
        ))}
    </BarItem>
  );
};
