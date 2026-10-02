<script setup lang="ts">
  import { computed } from 'vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { washOf, type BackgroundColor } from '~/Types/DesignSystem'
  import { sheetTearFor } from '~/Utilities/BrandColor'

  export interface CardProps {
    cardColor?: BackgroundColor
    tear?: 1 | 2 | 3 | 4 | 5 | 6
    seed?: string
  }

  export interface CardSlots {
    default?: () => void
    drawer?: () => void
  }

  const { cardColor = 'bg-paper', tear, seed = '' } = defineProps<CardProps>()

  const { drawer } = defineSlots<CardSlots>()

  const resolvedTear = computed(() => tear ?? sheetTearFor(seed))
</script>

<template>
  <KccSheet
    :wash="washOf(cardColor)"
    :at="{ x: '100%', y: '0%', w: '38%', h: 'min(40%, 68px)' }"
    :tear="resolvedTear"
    class="group/card kcc-slip--fill"
  >
    <div class="flex h-full flex-col">
      <slot />

      <div
        v-if="drawer"
        data-card-drawer
        class="h-[0%] overflow-hidden rounded-md bg-paper-2 text-ink transition-all group-hover/card:h-full focus-within:h-full"
      >
        <div class="p-4">
          <slot name="drawer" />
        </div>
      </div>
    </div>
  </KccSheet>
</template>
