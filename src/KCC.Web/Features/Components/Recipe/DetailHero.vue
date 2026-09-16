<!-- #region DetailHero Component Properties -->
<script lang="ts">
  import AccentTile from './AccentTile.vue'
  import RatingSummary from '~/Components/StarRating/RatingSummary.vue'
  import { ResourceString } from '~/Components/ResourceStrings'

  /**
   * Page-heading block for a recipe or a variant: tile, title, rating line, and description.
   */
  export default {
    name: 'DetailHero',
  }

  export interface DetailHeroProps {
    title: string
    /**
     * Passed to the AccentTile, which derives its fallback background color from it.
     */
    seed: string
    description: string
    /**
     * Font Awesome classes for the tile when there is no image.
     */
    icon?: string
    image?: string
    authorName?: string
    /**
     * 0–5. Only shown once `reviewCount` is non-zero.
     */
    averageRating?: number
    reviewCount?: number
    timesCooked?: number
  }

  export interface DetailHeroSlots {
    /**
     * Small uppercase kicker above the title.
     */
    eyebrow?: () => void
    footer?: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { title, seed, description, icon, image, authorName, averageRating, reviewCount, timesCooked } =
    defineProps<DetailHeroProps>()
  const { eyebrow } = defineSlots<DetailHeroSlots>()
</script>

<template>
  <div class="my-4 grid gap-4 rounded-3xl bg-paper p-6 text-ink lg:grid-cols-4">
    <AccentTile :seed :icon :image :alt="title" class="h-40 w-full text-7xl lg:size-full" />

    <div class="lg:col-span-3">
      <p v-if="eyebrow" class="mb-1 text-sm text-ink-soft uppercase">
        <slot name="eyebrow" />
      </p>

      <h1>{{ title }}</h1>

      <div class="mt-2 flex flex-wrap items-center gap-4">
        <span class="inline-flex items-center gap-1">
          <template v-if="reviewCount">
            <RatingSummary :value="averageRating ?? 0" strong />
            <span class="text-ink-soft">({{ reviewCount }} <ResourceString for="Reviews" />)</span>
          </template>
          <template v-else>
            <span class="text-ink-soft"><ResourceString for="NoRatingsYet" /></span>
          </template>
        </span>

        <span v-if="timesCooked" data-testid="times-cooked" class="inline-flex items-center gap-1">
          <i class="fa-duotone fa-fire-burner text-danger-ink"></i>
          <span>{{ timesCooked }} <ResourceString for="TimesCooked" /></span>
        </span>

        <span v-if="authorName" class="text-ink-soft"><ResourceString for="By" /> {{ authorName }}</span>
      </div>

      <p class="mt-4 max-w-[80ch] text-lg">{{ description }}</p>

      <slot name="footer" />
    </div>
  </div>
</template>
