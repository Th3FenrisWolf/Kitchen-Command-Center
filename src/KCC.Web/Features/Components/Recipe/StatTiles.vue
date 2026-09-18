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
     * Marks the tile as a status indicator (e.g. 'green' for a difficulty level). Currently only
     * sets the `difficulty-dot` hook on the value element; Task 42 turns it into a status well.
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
</script>

<template>
  <div data-testid="variant-stats" class="kcc-stats">
    <div v-for="(tile, index) in tiles" :key="index">
      <p class="kcc-lbl">{{ tile.label }}</p>
      <p class="kcc-v" :data-testid="tile.dotColor ? 'difficulty-dot' : undefined">
        <i v-if="tile.icon && !tile.dotColor" :class="tile.icon" aria-hidden="true"></i>
        {{ tile.value ?? '—' }}<small v-if="tile.unit && tile.value != null">{{ tile.unit }}</small>
      </p>
    </div>
  </div>
</template>
