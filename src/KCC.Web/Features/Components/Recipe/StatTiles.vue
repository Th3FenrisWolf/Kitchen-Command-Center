<script lang="ts">
  export interface StatTileSpec {
    /** Font Awesome class for the top icon (ignored when `dotColor` is set). */
    icon?: string
    /** Design-token name for a status dot instead of an icon (e.g. 'green'). */
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
</script>

<script setup lang="ts">
  import Badge from '~/Components/Badge/Badge.vue'

  defineProps<{ tiles: StatTileSpec[] }>()
</script>

<template>
  <section class="grid grid-cols-2 gap-4 lg:mx-4 lg:grid-cols-4">
    <div
      v-for="(tile, index) in tiles"
      :key="index"
      class="justify-center rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary"
    >
      <div class="flex items-center justify-center gap-4">
        <i v-if="tile.dotColor" :class="`fa-solid fa-circle fa-xs text-${tile.dotColor}`"></i>
        <i v-else :class="tile.icon" class="text-onyx-light"></i>

        <div v-if="tile.comingSoon">
          <Badge class="bg-bone-dark text-onyx-light">{{ tile.value }}</Badge>
        </div>
        <div v-else class="mt-1.5 font-casual text-2xl lg:text-3xl">
          {{ tile.value ?? '—' }}<span v-if="tile.unit && tile.value != null" class="ml-1 text-base">{{ tile.unit }}</span>
        </div>
      </div>

      <div class="mt-1 text-sm text-onyx-light">{{ tile.label }}</div>
    </div>
  </section>
</template>
