<!-- #region StatTiles Component Properties -->
<script lang="ts">
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
     * Turns the value into a status well tinted with this wash — `green`, `yellow` or `red`; any other
     * name takes the neutral well. Named for the `difficulty-dot` hook e2e still locates the value by.
     */
    dotColor?: string
    /** The big value. `null`/`undefined` renders as an em dash. */
    value?: string | number | null
    /** Small unit suffix shown after the value (e.g. 'min'). */
    unit?: string
    /** Caption above the value. */
    label: string
  }

  export interface StatTilesProps {
    tiles: StatTileSpec[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<StatTilesProps>()

  const WELLS: Record<string, string> = {
    green: 'kcc-well--success',
    yellow: 'kcc-well--warning',
    red: 'kcc-well--danger',
  }

  // w-fit so the tint hugs the value instead of banding the whole stat column.
  const well = (tile: StatTileSpec) => (tile.dotColor ? ['kcc-well', WELLS[tile.dotColor], 'w-fit'] : undefined)
</script>

<template>
  <div data-testid="variant-stats" class="kcc-stats">
    <div v-for="(tile, index) in tiles" :key="index">
      <p class="kcc-lbl">{{ tile.label }}</p>
      <p class="kcc-v" :class="well(tile)" :data-testid="tile.dotColor ? 'difficulty-dot' : undefined">
        <i v-if="tile.icon && !tile.dotColor" :class="tile.icon" aria-hidden="true"></i>
        {{ tile.value ?? '—' }}<small v-if="tile.unit && tile.value != null">{{ tile.unit }}</small>
      </p>
    </div>
  </div>
</template>
