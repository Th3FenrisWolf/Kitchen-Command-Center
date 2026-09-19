<!-- #region Card Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { washOf, type BackgroundColor } from '~/Types/DesignSystem'
  import { sheetTearFor } from '~/Utilities/BrandColor'

  /**
   * Widget card whose drawer expands on hover and focus.
   */
  export default {
    name: 'Card',
  }

  export interface CardProps {
    /**
     * The editor's colour, pooled as the sheet's wash. A ground carries none.
     * @default 'bg-paper'
     */
    cardColor?: BackgroundColor

    /** Neighbours never share a tear: a grid passes `(index % 6) + 1`. Unset, the tear comes from `seed`. */
    tear?: 1 | 2 | 3 | 4 | 5 | 6

    /**
     * Hashed into a stable tear when no grid index supplies one; usually the card's heading.
     * @default ''
     */
    seed?: string

    /**
     * @default ''
     */
    marginClasses?: string
  }

  export interface CardSlots {
    default?: () => void

    /**
     * Filling this slot is what gives the card a drawer and its hover behavior.
     */
    drawer?: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { cardColor = 'bg-paper', tear, seed = '', marginClasses = '' } = defineProps<CardProps>()

  const { drawer } = defineSlots<CardSlots>()

  const resolvedTear = computed(() => tear ?? sheetTearFor(seed))
</script>

<template>
  <KccSheet
    :wash="washOf(cardColor)"
    :at="{ x: '85%', y: '90%', w: '55%', h: '50%' }"
    :tear="resolvedTear"
    :class="['group/card kcc-slip--fill', marginClasses]"
  >
    <div class="flex h-full flex-col">
      <div
        :class="[
          'relative top-1 transition-all',
          drawer && 'group-focus-within/card:top-0 group-hover/card:top-0',
          drawer && 'group-focus-within/card:duration-100 group-hover/card:duration-100',
        ]"
      >
        <slot />
      </div>

      <div
        v-if="drawer"
        data-card-drawer
        class="h-[0%] overflow-hidden rounded-md bg-paper-2 text-ink transition-all group-hover/card:h-full focus-within:h-full"
      >
        <div class="p-4">
          <slot name="drawer" />
        </div>
      </div>
    </div>
  </KccSheet>
</template>

<style lang="css">
  .ktc-widget-body-wrapper:has(> .group\/card) {
    display: grid;
  }
</style>
