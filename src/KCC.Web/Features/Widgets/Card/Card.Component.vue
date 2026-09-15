<!-- #region Card Component Properties -->
<script lang="ts">
  import { computed, type ComputedRef } from 'vue'
  import { toBackgroundColor, toTextColor, type BackgroundColor, type TextColor } from '~/Types/DesignSystem'

  /**
   * Widget card whose drawer expands on hover and focus.
   */
  export default {
    name: 'Card',
  }

  export interface CardProps {
    /**
     * @default 'bg-surface-500'
     */
    cardColor?: BackgroundColor

    /**
     * @default 'text-bone'
     */
    cardTextColor?: TextColor

    /**
     * Inverts against the card by default, so the drawer reads as a cut-out.
     * @default cardTextColor
     */
    drawerColor?: BackgroundColor | null

    /**
     * @default cardColor
     */
    drawerTextColor?: TextColor | null

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
  const {
    cardColor = 'bg-surface-500',
    cardTextColor = 'text-bone',
    drawerColor = null,
    drawerTextColor = null,
    marginClasses = '',
  } = defineProps<CardProps>()

  const { drawer } = defineSlots<CardSlots>()

  const resolvedDrawerColor: ComputedRef<BackgroundColor> = computed(() => {
    return drawerColor ?? toBackgroundColor(cardTextColor)
  })

  const resolvedDrawerTextColor: ComputedRef<TextColor> = computed(() => {
    return drawerTextColor ?? toTextColor(cardColor)
  })
</script>

<template>
  <div
    :class="[
      'group/card flex flex-col justify-center gap-2 rounded-3xl p-4 text-center shadow-primary transition-all',
      drawer && 'focus-within:shadow-primary-raised hover:shadow-primary-raised',
      cardColor,
      cardTextColor,
      marginClasses,
    ]"
  >
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
      :class="[
        'h-[0%] content-center overflow-hidden rounded-2xl transition-all',
        'group-hover/card:h-full focus-within:h-full',
        resolvedDrawerColor,
        resolvedDrawerTextColor,
      ]"
    >
      <div class="p-4">
        <slot name="drawer" />
      </div>
    </div>
  </div>
</template>

<style lang="css">
  .ktc-widget-body-wrapper:has(> .group\/card) {
    display: grid;
  }
</style>
