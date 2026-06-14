<script lang="ts">
  /**
   * A card component that supports functionality for expanding via hover
   *
   * @param {string} [cardColor='bg-surface-500'] - The color of the card background
   * @param {string} [cardTextColor='text-bone'] - The color of the card text
   * @param {string} [drawerColor=null] - The color of the card drawer background, if null, the cardTextColor will be used
   * @param {string} [drawerTextColor=null] - The color of the card drawer text, if null, the cardColor will be used
   * @param {string} [marginClasses=''] - The margin classes to apply to the card
   *
   * @slot default — Main content of the card
   * @slot drawer — Content to show when the card is expanded
   */

  import type { BackgroundColor, TextColor } from '~/Types/DesignSystem'

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
</script>

<script setup lang="ts">
  import { computed, useSlots } from 'vue'

  const {
    cardColor = 'bg-surface-500',
    cardTextColor = 'text-bone',
    drawerColor = null,
    drawerTextColor = null,
    marginClasses = '',
  } = defineProps<CardProps>()

  const hasDrawer = !!useSlots().drawer

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
      'group/card flex flex-col justify-center gap-2 rounded-3xl p-4 text-center shadow-primary transition-all duration-300',
      hasDrawer && 'focus-within:shadow-primary-raised hover:shadow-primary-raised',
      cardColor,
      cardTextColor,
      marginClasses,
    ]"
  >
    <div
      :class="[
        'relative top-1 transition-all duration-300',
        hasDrawer && 'group-focus-within/card:top-0 group-hover/card:top-0',
        hasDrawer && 'group-focus-within/card:duration-100 group-hover/card:duration-100',
      ]"
    >
      <slot />
    </div>

    <div
      v-if="hasDrawer"
      :class="[
        'h-[0%] content-center overflow-hidden rounded-2xl transition-all duration-300',
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
