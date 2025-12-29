<script setup lang="ts">
import { cx } from '~/utilities/cx'

import { onMounted, ref } from 'vue'

const containerRef = ref<HTMLElement>()

onMounted(() => {
  const observer = new IntersectionObserver(
    ([e]) => e.target.nextElementSibling?.classList.toggle('stuck', e.boundingClientRect.top < 0),
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
    title: string
    description: string
    color: string
  }[]
}>()
</script>

<template>
  <div ref="containerRef" class="grid items-center gap-8">
    <div
      v-for="(card, index) in props.cards"
      :key="card.title"
      data-card
      :class="cx('sticky', index === props.cards.length - 1 && 'last')"
      :style="`top: ${32 * (index + 1)}px`"
    >
      <div data-sentinel class="absolute size-0" :style="`top: -${32 * (index + 1) + 1}px`"></div>
      <div
        :class="
          cx(
            'shadow-primary [&.stuck]:shadow-light aspect-square origin-top rounded-3xl p-8 text-center transition-all duration-100 [&.stuck]:scale-95 [&:is(.last_*)]:scale-100',
            card.color,
          )
        "
      >
        <h2 class="text-heading">{{ card.title }}</h2>
        <p class="text-balance">{{ card.description }}</p>
      </div>
    </div>
  </div>
</template>
