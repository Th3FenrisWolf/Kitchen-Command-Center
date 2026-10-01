<script setup lang="ts">
  export interface StatTileSpec {
    icon?: string
    // Named for the `difficulty-dot` hook the E2E suite locates the value by.
    dotColor?: string
    value?: string | number | null
    unit?: string
    label: string
  }

  export interface StatTilesProps {
    tiles: StatTileSpec[]
  }

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
