<script setup lang="ts">
  import { tileTearFor, washFor } from '~/Utilities/BrandColor'

  export interface AccentTileProps {
    /**
     * Picks the fallback wash and tear deterministically, so the same subject always tiles alike.
     */
    seed: string
    /**
     * Font Awesome classes for the fallback tile.
     */
    icon?: string
    image?: string
    /**
     * Falls back to `seed` when omitted.
     */
    alt?: string
    /**
     * The larger tile size, for the detail hero.
     */
    large?: boolean
  }

  const { seed, icon, image, alt, large } = defineProps<AccentTileProps>()
</script>

<template>
  <img v-if="image" :src="image" :alt="alt ?? seed" class="block object-cover" />
  <div v-else class="kcc-torn">
    <div
      class="kcc-tile"
      :class="[`kcc-tear-tile-${tileTearFor(seed)}`, large && 'kcc-tile--lg']"
      :style="{ '--c': `var(--color-${washFor(seed)})` }"
      aria-hidden="true"
    >
      <i :class="icon"></i>
    </div>
  </div>
</template>
