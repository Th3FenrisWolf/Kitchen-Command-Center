<!-- #region DetailHero Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import AccentTile from './AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import { washFor } from '~/Utilities/BrandColor'
  import { ResourceString } from '~/Components/ResourceStrings'

  /**
   * Page-heading block for a recipe or a variant: a hero-torn slip with the subject's wash under the left
   * margin, a large tile pinned over the corner, and the description beside the title.
   */
  export default {
    name: 'DetailHero',
  }

  export interface DetailHeroProps {
    title: string
    /**
     * Picks the sheet's wash and the tile, so the same subject is always coloured alike.
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

  const wash = computed(() => ({
    '--c': `var(--color-${washFor(seed)})`,
    '--x': '10%',
    '--y': '15%',
    '--w': '45%',
    '--h': '80%',
  }))

  const ratingText = computed(() => formatRating(averageRating ?? 0))
</script>

<template>
  <div class="kcc-slip kcc-recipe kcc-tear-hero">
    <div class="kcc-torn">
      <div class="kcc-sheet" style="--pad: 48px">
        <span class="kcc-wash" :style="wash" aria-hidden="true"></span>

        <div class="grid gap-x-7 gap-y-6 sm:grid-cols-2">
          <div>
            <p v-if="eyebrow" class="kcc-kick"><slot name="eyebrow" /></p>

            <h1 class="kcc-h3">{{ title }}</h1>

            <p class="kcc-kick flex flex-wrap items-center gap-x-4 text-ink">
              <span v-if="reviewCount" class="inline-flex items-center gap-2">
                <i class="fa-duotone fa-star" aria-hidden="true"></i>
                <span class="kcc-num" role="img" :aria-label="`${ratingText} of 5 stars`">{{ ratingText }}</span>
                <span class="text-ink-soft">
                  <span class="kcc-num">· {{ reviewCount }}</span> <ResourceString for="Reviews" />
                </span>
              </span>
              <span v-else class="text-ink-soft"><ResourceString for="NoRatingsYet" /></span>

              <span v-if="timesCooked" data-testid="times-cooked" class="inline-flex items-center gap-2">
                <i class="fa-duotone fa-fire-burner" aria-hidden="true"></i>
                <span>
                  <span class="kcc-num">{{ timesCooked }}</span> <ResourceString for="TimesCooked" />
                </span>
              </span>

              <span v-if="authorName" class="text-ink-soft"><ResourceString for="By" /> {{ authorName }}</span>
            </p>
          </div>

          <div>
            <p class="kcc-body max-w-[80ch]">{{ description }}</p>
            <slot name="footer" />
          </div>
        </div>
      </div>
    </div>

    <div class="kcc-tilewrap" style="top: -22px; left: 40px">
      <AccentTile :seed :icon :image :alt="title" large class="size-24" />
      <span class="kcc-tape" aria-hidden="true"></span>
    </div>

    <span class="kcc-tape" aria-hidden="true"></span>
  </div>
</template>
