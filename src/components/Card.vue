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
import cx from '~/utilities/cx'

const { dark = false } = defineProps<CardProps>()
</script>

<template>
  <div
    :class="
      cx(
        'shadow-primary group/card flex flex-col justify-center gap-2 rounded-3xl p-4 text-center transition-all duration-300',
        'focus-within:shadow-primary-raised hover:shadow-primary-raised',
        dark ? 'bg-base text-bone' : 'bg-bone text-onyx',
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
          dark ? 'bg-bone text-onyx' : 'bg-base text-bone',
        )
      "
    >
      <div class="p-4">
        <slot name="drawer" />
      </div>
    </div>
  </div>
</template>
