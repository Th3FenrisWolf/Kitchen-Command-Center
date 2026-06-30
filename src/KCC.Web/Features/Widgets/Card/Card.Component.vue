<script lang="ts">
  import type { BackgroundColor, TextColor } from '~/Types/DesignSystem'

  /**
   * A card component that supports functionality for expanding via hover
   */
  export default {
    name: 'Card',
  }

  export interface CardProps {
    /**
     * The color of the card background
     * @default 'bg-surface-500'
     */
    cardColor?: BackgroundColor

    /**
     * The color of the card text
     * @default 'text-bone'
     */
    cardTextColor?: TextColor

    /**
     * The color of the card drawer background
     * @default cardTextColor
     */
    drawerColor?: BackgroundColor | null

    /**
     * The color of the card drawer text
     * @default cardColor
     */
    drawerTextColor?: TextColor | null

    /**
     * The margin classes to apply to the card
     * @default ''
     */
    marginClasses?: string
  }

  export interface CardSlots {
    /**
     * The content to display in the card
     */
    default: () => void

    /**
     * The content to display in the card drawer
     */
    drawer: () => void
  }
</script>

<script setup lang="ts">
  import { computed } from 'vue'

  const {
    cardColor = 'bg-surface-500',
    cardTextColor = 'text-bone',
    drawerColor = null,
    drawerTextColor = null,
    marginClasses = '',
  } = defineProps<CardProps>()

  const { drawer } = defineSlots<CardSlots>()

  const resolvedDrawerColor = computed(() => {
    return drawerColor ?? cardTextColor
  })

  const resolvedDrawerTextColor = computed(() => {
    return drawerTextColor ?? cardColor
  })
</script>

<template>
  <div
    :class="[
      'group/card flex flex-col justify-center gap-2 rounded-3xl p-4 text-center shadow-primary transition-all',
      !!drawer && 'focus-within:shadow-primary-raised hover:shadow-primary-raised',
      cardColor,
      cardTextColor,
      marginClasses,
    ]"
  >
    <div
      :class="[
        'relative top-1 transition-all',
        !!drawer && 'group-focus-within/card:top-0 group-hover/card:top-0',
        !!drawer && 'group-focus-within/card:duration-100 group-hover/card:duration-100',
      ]"
    >
      <slot />
    </div>

    <div
      v-if="!!drawer"
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
