<!-- #region RecipeCard Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  /**
   * Grid card for a recipe or a variant, with its hero stat raised into a notch.
   */
  export default {
    name: 'RecipeCard',
  }

  export interface RecipeCardProps {
    /**
     * Build with `hitToCard` or `variantToCard` from recipeCardModel.
     */
    card: RecipeCardModel
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { card } = defineProps<RecipeCardProps>()

  const bodyMeta = computed(() => (card.meta ?? []).filter((item) => item.key !== card.notch.stat))
  const unratedLabel = computed(() => (card.rating?.count === 0 ? card.rating.emptyLabel : undefined))
</script>

<template>
  <AppLink
    :href="card.href"
    v-bind="card.dataAttrs"
    class="recipe-card grid grid-rows-subgrid rounded-3xl bg-paper-2 p-3 text-ink no-underline transition-all focus-within:-translate-y-1 hover:-translate-y-1"
  >
    <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="recipe-card-tile w-full text-5xl" />

    <span class="recipe-card-panel relative grid grid-rows-subgrid rounded-2xl bg-paper px-4 pt-2 pb-4 text-ink">
      <span class="recipe-card-notch absolute left-0 bg-paper">
        <span
          class="recipe-card-notch-pill absolute flex w-full items-center justify-center gap-1.5 overflow-hidden rounded-full bg-paper-2 px-3 font-bold whitespace-nowrap text-ink"
          :data-testid="card.notch.stat === 'rating' ? 'recipe-card-rating' : undefined"
          :data-average-rating="card.notch.stat === 'rating' ? card.rating?.average : undefined"
        >
          <i :class="[card.notch.icon, card.notch.stat === 'rating' && 'text-rating-ink']"></i> {{ card.notch.text }}
        </span>
      </span>

      <span v-if="card.eyebrow" class="row-start-1 mt-2 text-xs font-bold tracking-wide text-ink/60 uppercase">{{
        card.eyebrow
      }}</span>
      <span class="row-start-2 mt-2 font-casual text-2xl">{{ card.name }}</span>

      <span
        v-if="unratedLabel || bodyMeta.length"
        class="row-start-3 mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-ink/70"
      >
        <span v-if="unratedLabel" class="italic">{{ unratedLabel }}</span>
        <span v-for="(item, i) in bodyMeta" :key="i"><i v-if="item.icon" :class="item.icon"></i> {{ item.text }}</span>
      </span>

      <span v-if="card.subtitle" class="row-start-4 mt-2 text-sm text-ink/70">{{ card.subtitle }}</span>
      <p v-if="card.description" class="row-start-5 mt-2 text-sm text-ink/85">{{ card.description }}</p>

      <span v-if="card.tags.length" class="row-start-6 mt-3 flex flex-wrap gap-1">
        <Badge v-for="tag in card.tags" :key="tag" class="bg-paper-2/15 text-ink">{{ tag }}</Badge>
      </span>
    </span>
  </AppLink>
</template>

<style scoped>
  /*
   * How far the notch has climbed out of the panel, 0 to 1. Every edge of both shapes is derived from it,
   * so one interpolated value keeps the tile's cut and the notch itself in step. It is a number rather
   * than a length because the sweep maths below needs it as a ratio, and CSS cannot divide two lengths.
   * Registering it is what makes it interpolate at all.
   */
  @property --notch-progress {
    inherits: true;
    initial-value: 0;
    syntax: '<number>';
  }

  .recipe-card {
    --notch-width: 8rem;
    --notch-radius: 1rem;
    /* Bone band between the notch and the tile's cut, and the seam between the tile and the panel. */
    --notch-gap: 0.5rem;
    /*
     * Concave sweep where the notch's right edge meets the panel's top edge. Deliberately looser than the
     * convex corner it pairs with — set to the same radius it reads tighter than that corner.
     */
    --notch-flare: 1.5rem;
    /* Matches AccentTile's own rounding, so the corner the cut leaves reads like the tile's others. */
    --notch-tile-radius: 1rem;
    /* Centres the pill in the notch at full open. */
    --notch-inset: 0.5rem;
    --notch-progress: 0;

    /*
     * The notch's right side is one S of two tangent arcs — convex out of the flat top, concave into the
     * panel's edge — so its rise is exactly radius + flare. Holding --notch-height to that sum makes the
     * progress value double as the arc parameter (1 - cos of the swept angle), which is what lets both
     * radii stay full size and simply sweep a smaller angle as the notch opens. Shrinking the radii
     * instead would leave a shallow notch reading as a tight little step rather than a gentle blend.
     */
    --notch-height: calc(var(--notch-radius) + var(--notch-flare));
    --notch-open: calc(var(--notch-height) * var(--notch-progress));
    --notch-sin: sqrt(2 * var(--notch-progress) - pow(var(--notch-progress), 2));

    /* Where the flat top gives way to the S, and how far the S has run at the current sweep. */
    --notch-shoulder: calc(var(--notch-width) - var(--notch-radius));
    --notch-turn-x: calc(var(--notch-shoulder) + var(--notch-radius) * var(--notch-sin));
    --notch-turn-y: calc(var(--notch-radius) * var(--notch-progress));
    --notch-run: calc(var(--notch-shoulder) + var(--notch-height) * var(--notch-sin));
    /* The same turn, offset outward by the band — the cut's two arcs are concentric with the notch's. */
    --notch-cut-turn: calc(var(--notch-shoulder) + (var(--notch-radius) + var(--notch-gap)) * var(--notch-sin));

    /* Overrides the transition-shadow utility's property list, not its duration or easing. */
    transition-property: box-shadow, translate, --notch-progress;
  }

  .recipe-card:hover,
  .recipe-card:focus-within {
    --notch-progress: 1;
  }

  @media (prefers-reduced-motion: reduce) {
    .recipe-card {
      transition-property: box-shadow;
    }
  }

  /*
   * Reaches down to the panel, and stays that tall in both states, so the card holds its height. Square
   * outer corners in the path — border-radius still rounds those, and clipping is their intersection —
   * which leaves the path describing only the cut. That cut is the notch's outline offset outward by
   * --notch-gap: the convex arc grows by the gap, the concave one shrinks by it, and both share the
   * notch's centres, which is what holds the band to a constant width at every depth.
   */
  .recipe-card-tile {
    clip-path: shape(
      from 0 0,
      line to 100% 0,
      line to 100% 100%,
      line to var(--notch-run) 100%,
      arc to var(--notch-cut-turn)
        calc(100% - var(--notch-open) + (var(--notch-radius) + var(--notch-gap)) * var(--notch-progress)) of
        calc(var(--notch-flare) - var(--notch-gap)) cw,
      arc to var(--notch-shoulder) calc(100% - var(--notch-open)) of calc(var(--notch-radius) + var(--notch-gap)) ccw,
      line to var(--notch-tile-radius) calc(100% - var(--notch-open)),
      arc to 0 calc(100% - var(--notch-open) - var(--notch-tile-radius)) of var(--notch-tile-radius) cw,
      close
    );
    grid-row: 1;
    height: calc(10rem + var(--notch-height));
  }

  /*
   * Both the card and this panel are subgrids, so the slots inside reach all the way out to
   * RecipeCardGrid's tracks and line up with the other cards in the row. Slot rows are numbered from
   * the panel's own first spanned track, which is why they run 1 to 6 while the tile above holds the
   * card's row 1. Slots space themselves with top margins rather than a row gap — the gap would come
   * from the grid, so it would apply to the tile seam and to slots this page leaves empty too. The
   * panel's own top padding lands inside that first track, so it and a slot's margin together make
   * up the inset above whichever slot comes first.
   */
  .recipe-card-panel {
    /* Held equal to the notch's own, since the notch paints over this corner and has to match it. */
    border-top-left-radius: var(--notch-radius);
    grid-row: 2 / -1;
    margin-top: var(--notch-gap);
  }

  /*
   * Wider than the notch by --notch-flare so the flare has somewhere to live, and taller by --notch-radius
   * so the top-left corner always has room for its full radius no matter how little the notch has opened.
   * That foot is kept to exactly --notch-radius: it has to reach far enough to paint over the panel's own
   * top-left corner, so the corner on screen is always this one and rides up with the notch, but this
   * element is positioned and so paints above the panel's text — any deeper and it would cover the title.
   * The clip-path also clips the pill, which is what slices it as it rises past the edge.
   */
  .recipe-card-notch {
    bottom: calc(100% - var(--notch-radius));
    clip-path: shape(
      from 0 var(--notch-radius),
      arc to var(--notch-radius) 0 of var(--notch-radius) cw,
      line to var(--notch-shoulder) 0,
      arc to var(--notch-turn-x) var(--notch-turn-y) of var(--notch-radius) cw,
      arc to var(--notch-run) var(--notch-open) of var(--notch-flare) ccw,
      line to var(--notch-run) 100%,
      line to 0 100%,
      close
    );
    height: calc(var(--notch-open) + var(--notch-radius));
    width: calc(var(--notch-width) + var(--notch-flare));
  }

  /*
   * Held a fixed distance above the panel's top edge, so the notch growing past it does the revealing.
   * Sized to its label rather than to the notch: the notch has to be one fixed width for the paths
   * above, and stretching the pill to match would strand a short rating in a very wide bubble.
   */
  .recipe-card-notch-pill {
    bottom: calc(var(--notch-radius) + var(--notch-inset));
    height: calc(var(--notch-height) - var(--notch-inset) * 2);
    left: var(--notch-inset);
    max-width: calc(var(--notch-width) - var(--notch-inset) * 2);
  }
</style>
