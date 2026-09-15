<!-- #region Stacker Component Properties -->
<script lang="ts">
  import { onMounted, ref } from 'vue'
  import type { BackgroundColor } from '~/Types/DesignSystem'

  /**
   * Column of sticky cards that shrink as the next one scrolls over them.
   */
  export default {
    name: 'Stacker',
  }

  export interface StackerCard {
    heading: string
    subHeading: string
    backgroundColor: BackgroundColor
  }

  export interface StackerProps {
    /**
     * Rendered in order; each card pins 32px lower than the one before it.
     */
    cards: StackerCard[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const containerRef = ref<HTMLElement>()

  onMounted(() => {
    const observer = new IntersectionObserver(
      ([e]) => e?.target.nextElementSibling?.classList.toggle('stuck', e.boundingClientRect.top < 0),
      { threshold: [1] },
    )

    if (containerRef.value) {
      const sentinels = containerRef.value.querySelectorAll('[data-sentinel]')
      sentinels.forEach((sentinel) => observer.observe(sentinel))
    }
  })

  const props = defineProps<StackerProps>()
</script>

<template>
  <div ref="containerRef" class="grid items-center gap-8">
    <div
      v-for="(card, index) in props.cards"
      :key="card.heading"
      data-card
      :class="['sticky', index === props.cards.length - 1 && 'last']"
      :style="`top: ${32 * (index + 1)}px`"
    >
      <div data-sentinel class="absolute size-0" :style="`top: -${32 * (index + 1) + 1}px`"></div>
      <div
        :class="[
          'aspect-square origin-top rounded-3xl p-8 text-center shadow-primary transition-all duration-100 [.stuck]:scale-95 [.stuck]:shadow-none',
          '[.last_div]:scale-100 [.last_div]:shadow-primary',
          card.backgroundColor,
        ]"
      >
        <h2 class="text-4.5xl">{{ card.heading }}</h2>
        <p class="text-balance">{{ card.subHeading }}</p>
      </div>
    </div>
  </div>
</template>
