<!-- #region AccentTile Component Properties -->
<script lang="ts">
  export interface AccentTileProps {
    /**
     * The seed string to use for generating the background color
     */
    seed: string
    /**
     * The icon class to use for the tile
     */
    icon?: string
    /**
     * The image URL to use for the tile
     */
    image?: string
    /**
     * The alt text to use for the image
     */
    alt?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { backgroundColorFor } from '~/Utilities/BrandColor'

  const { seed, icon, image, alt } = defineProps<AccentTileProps>()
</script>

<template>
  <img v-if="image" :src="image" :alt="alt ?? seed" class="rounded-2xl object-cover" />
  <div v-else class="grid place-items-center rounded-2xl text-onyx" :class="backgroundColorFor(seed)" aria-hidden="true">
    <i :class="icon"></i>
  </div>
</template>
