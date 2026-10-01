<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import { washOf, type BackgroundColor } from '~/Types/DesignSystem'
  import { listTearFor } from '~/Utilities/BrandColor'
  import { markStuckCards } from '~/Widgets/Stacker/markStuckCards'

  export interface StackerCard {
    heading: string
    subHeading: string
    backgroundColor: BackgroundColor

    /** Set when the page runs one tear cycle across its sheets; unset, the stack cycles from the first preset. */
    tear?: Exclude<Tear, 'hero'>
  }

  export interface StackerProps {
    /**
     * Rendered in order; each card pins 32px lower than the one before it.
     */
    cards: StackerCard[]
  }

  const containerRef = ref<HTMLElement>()

  // The slip is the sentinel's next sibling, so it is the slip that takes `.stuck` and shrinks. `scale-*`
  // sets the `scale` property, which composes with the slip's `rotate` rather than replacing it.
  onMounted(() => {
    const observer = new IntersectionObserver(markStuckCards, { threshold: [1] })

    if (containerRef.value) {
      const sentinels = containerRef.value.querySelectorAll('[data-sentinel]')
      sentinels.forEach((sentinel) => observer.observe(sentinel))
    }
  })

  const props = defineProps<StackerProps>()
</script>

<template>
  <div ref="containerRef" class="grid items-center gap-9">
    <div
      v-for="(card, index) in props.cards"
      :key="card.heading"
      data-card
      :class="['sticky', index === props.cards.length - 1 && 'last']"
      :style="`top: ${32 * (index + 1)}px`"
    >
      <div data-sentinel class="absolute size-0" :style="`top: -${32 * (index + 1) + 1}px`"></div>
      <KccSheet
        :tear="card.tear ?? listTearFor(index)"
        :wash="washOf(card.backgroundColor)"
        :at="{ x: '85%', y: '90%', w: '55%', h: '50%' }"
        class="origin-top transition-all duration-100 [.last_div]:scale-100 [.stuck]:scale-95"
      >
        <h3 class="kcc-h4">{{ card.heading }}</h3>
        <p class="kcc-body">{{ card.subHeading }}</p>
      </KccSheet>
    </div>
  </div>
</template>
