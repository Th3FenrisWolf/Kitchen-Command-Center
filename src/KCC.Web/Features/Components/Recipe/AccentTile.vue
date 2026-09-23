<!-- #region AccentTile Component Properties -->
<script lang="ts">
  import { backgroundColorFor } from '~/Utilities/BrandColor'

  /**
   * Square thumbnail: the image when there is one, otherwise an icon on a brand-colored ground.
   */
  export default {
    name: 'AccentTile',
  }

  export interface AccentTileProps {
    /**
     * Picks the fallback background color deterministically, so the same subject always tiles alike.
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
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { seed, icon, image, alt } = defineProps<AccentTileProps>()
</script>

<template>
  <img v-if="image" :src="image" :alt="alt ?? seed" class="rounded-2xl object-cover" />
  <div v-else class="sk-tile grid place-items-center rounded-2xl" :class="backgroundColorFor(seed)" aria-hidden="true">
    <i :class="icon"></i>
  </div>
</template>
