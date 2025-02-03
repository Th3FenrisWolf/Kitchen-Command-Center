<script lang="ts">
/**
 * A card component that supports functionality for expanding via hover
 *
 * @param {boolean} [dark=false] - Wether the Card will be dark theme
 *
 * @slot default — Main content of the card
 * @slot drawer — Content to show when the card is expanded
 */
export default {
  name: 'Card',
}

export interface CardProps {
  /** Wether the Card will be dark theme
   * @default false
   */
  dark?: boolean
}
</script>

<script setup lang="ts">
import clsx from 'clsx'

const { dark = false } = defineProps<CardProps>()
</script>

<template>
  <div class="h-60 content-center">
    <div
      :class="
        clsx(
          'shadow-primary group/card grid min-h-48 content-center gap-2 rounded-3xl p-4 text-center transition-all duration-300',
          'focus-within:shadow-primary-raised hover:shadow-primary-raised',
          dark ? 'bg-base text-bone' : 'bg-bone text-onyx',
        )
      "
    >
      <div>
        <slot />
      </div>

      <div
        :class="
          clsx(
            'h-0 content-center overflow-hidden rounded-2xl px-2 transition-all duration-300',
            'focus-within:h-28 focus-within:p-2 group-hover/card:h-28 group-hover/card:p-2',
            dark ? 'bg-bone text-onyx' : 'bg-base text-bone',
          )
        "
      >
        <slot name="drawer" />
      </div>
    </div>
  </div>
</template>
