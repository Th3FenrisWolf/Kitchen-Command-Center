<script setup lang="ts">
  import StatTile from './StatTile.vue'
  import type { StatTileSpec } from './types'

  defineProps<{ tiles: StatTileSpec[] }>()
</script>

<template>
  <section class="grid grid-cols-4 gap-3">
    <StatTile v-for="(tile, index) in tiles" :key="index" v-bind="tile" />
  </section>
</template>
