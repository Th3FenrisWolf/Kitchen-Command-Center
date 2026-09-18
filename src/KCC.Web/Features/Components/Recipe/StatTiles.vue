<!-- #region StatTiles Component Properties -->
<script lang="ts">
  import Badge from '~/Components/Badge/Badge.vue'

  /**
   * Row of at-a-glance stats under a detail hero.
   */
  export default {
    name: 'StatTiles',
  }

  export interface StatTileSpec {
    /** Font Awesome class for the leading icon (ignored when `dotColor` is set). */
    icon?: string
    /**
     * Renders a plain status dot instead of an icon (e.g. 'green' for a difficulty level). The
     * value only selects the dot; icons are never coloured, so it no longer tints anything.
     */
    dotColor?: string
    /** The big value. `null`/`undefined` renders as an em dash. */
    value?: string | number | null
    /** Small unit suffix shown after the value (e.g. 'min'). */
    unit?: string
    /** Caption under the value. */
    label: string
    /** When true, render `value` as a muted "coming soon" badge. */
    comingSoon?: boolean
  }

  export interface StatTilesProps {
    tiles: StatTileSpec[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<StatTilesProps>()
</script>

<template>
  <div data-testid="variant-stats" class="kcc-stats">
    <div v-for="(tile, index) in tiles" :key="index">
      <p class="kcc-lbl">{{ tile.label }}</p>
      <p class="kcc-v">
        <i v-if="tile.dotColor" data-testid="difficulty-dot" class="fa-solid fa-circle" aria-hidden="true"></i>
        <i v-else :class="tile.icon" aria-hidden="true"></i>
        <Badge v-if="tile.comingSoon">{{ tile.value }}</Badge>
        <template v-else
          >{{ tile.value ?? '—' }}<small v-if="tile.unit && tile.value != null">{{ tile.unit }}</small></template
        >
      </p>
    </div>
  </div>
</template>
