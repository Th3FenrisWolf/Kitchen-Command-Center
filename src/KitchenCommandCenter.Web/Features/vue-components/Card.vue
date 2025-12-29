<script lang="ts">
/**
 * A card component that supports functionality for expanding via hover
 *
 * @param {string} [cardColor='bg-base'] - The color of the card background
 * @param {string} [cardTextColor='text-bone'] - The color of the card text
 * @param {string} [drawerColor=null] - The color of the card drawer background, if null, the cardTextColor will be used
 * @param {string} [drawerTextColor=null] - The color of the card drawer text, if null, the cardColor will be used
 *
 * @slot default — Main content of the card
 * @slot drawer — Content to show when the card is expanded
 */

import type { BackgroundColor, TextColor } from '~/types/design-system'

export default {
  name: 'Card',
}

export interface CardProps {
  /**
   * The color of the card background
   * @default 'bg-base'
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
}
</script>

<script setup lang="ts">
import { computed } from 'vue'
import cx from '~/utilities/cx'

const {
  cardColor = 'bg-base',
  cardTextColor = 'text-bone',
  drawerColor = null,
  drawerTextColor = null,
} = defineProps<CardProps>()

const resolvedDrawerColor = computed(() => {
  return drawerColor ?? cardTextColor
})

const resolvedDrawerTextColor = computed(() => {
  return drawerTextColor ?? cardColor
})
</script>

<template>
  <div
    :class="
      cx(
        'shadow-primary group/card flex flex-col justify-center gap-2 rounded-3xl p-4 text-center transition-all duration-300',
        'focus-within:shadow-primary-raised hover:shadow-primary-raised',
        cardColor,
        cardTextColor,
      )
    "
  >
    <div
      :class="
        cx(
          'relative top-1 transition-all duration-300',
          'group-focus-within/card:top-0 group-hover/card:top-0',
          'group-focus-within/card:duration-100 group-hover/card:duration-100',
        )
      "
    >
      <slot />
    </div>

    <div
      :class="
        cx(
          'h-[0%] content-center overflow-hidden rounded-2xl transition-all duration-300',
          'focus-within:h-full group-hover/card:h-full',
          resolvedDrawerColor,
          resolvedDrawerTextColor,
        )
      "
    >
      <div class="p-4">
        <slot name="drawer" />
      </div>
    </div>
  </div>
</template>
