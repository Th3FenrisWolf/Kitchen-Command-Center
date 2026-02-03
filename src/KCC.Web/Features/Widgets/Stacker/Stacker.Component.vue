<script setup lang="ts">
import { cx } from '~/Utilities/CX'
import type { BackgroundColor } from '~/Types/DesignSystem'
import { onMounted, ref } from 'vue'

const containerRef = ref<HTMLElement>()

onMounted(() => {
  const observer = new IntersectionObserver(
    ([e]) => e?.target.nextElementSibling?.classList.toggle('stuck', e.boundingClientRect.top < 0),
    { threshold: [1] },
  )

  // Find all card elements within the container
  if (containerRef.value) {
    const sentinels = containerRef.value.querySelectorAll('[data-sentinel]')
    sentinels.forEach((sentinel) => observer.observe(sentinel))
  }
})

const props = defineProps<{
  cards: {
    heading: string
    subHeading: string
    backgroundColor: BackgroundColor
  }[]
}>()
</script>

<template>
  <div ref="containerRef" class="grid items-center gap-8">
    <div
      v-for="(card, index) in props.cards"
      :key="card.heading"
      data-card
      :class="cx('sticky', index === props.cards.length - 1 && 'last')"
      :style="`top: ${32 * (index + 1)}px`"
    >
      <div data-sentinel class="absolute size-0" :style="`top: -${32 * (index + 1) + 1}px`"></div>
      <div
        :class="
          cx(
            'aspect-square origin-top rounded-3xl p-8 text-center shadow-primary transition-all duration-100 [&.stuck]:scale-95 [&.stuck]:shadow-light [.last_*]:scale-100',
            card.backgroundColor,
          )
        "
      >
        <h2 class="text-heading">{{ card.heading }}</h2>
        <p class="text-balance">{{ card.subHeading }}</p>
      </div>
    </div>
  </div>
</template>
