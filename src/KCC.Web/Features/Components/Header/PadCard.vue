<script setup lang="ts">
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { Wash } from '~/Types/DesignSystem'
  import type { CardState } from './usePad'

  export interface PadCardProps {
    id: string
    state: CardState
    tear: Exclude<Tear, 'hero'>
    tilt: number
    labelledBy?: string
    label?: string
    width?: string
    left?: number
    wash?: Wash
    at?: { x?: string; y?: string; w?: string; h?: string }
  }

  const { id, state, tear, tilt, labelledBy, label, width, left, wash, at } = defineProps<PadCardProps>()
</script>

<template>
  <KccSheet
    :id
    class="pad-card"
    :class="`is-${state}`"
    :tear
    :wash
    :at
    role="region"
    :aria-labelledby="labelledBy"
    :aria-label="label"
    :inert="state !== 'open'"
    :style="{ '--r': tilt, '--pad-card-w': width, left: left === undefined ? undefined : `${left}px` }"
  >
    <div class="pt-6">
      <slot />
    </div>
  </KccSheet>
</template>

<style>
  .pad-card {
    --peek: 19px;
    --peek-r: -1.8deg;
    left: 40px;
    pointer-events: none;
    position: absolute;
    top: 0;
    transform: translateY(calc(-100% - 6px));
    transform-origin: 100% 0;
    transition:
      transform 280ms cubic-bezier(0.5, 0, 0.9, 0.35),
      visibility 0s linear 280ms;
    visibility: hidden;
    width: var(--pad-card-w, 600px);
  }

  .pad-card.is-peek {
    transform: translateY(calc(-100% + var(--peek))) rotate(var(--peek-r));
    transition:
      transform 300ms cubic-bezier(0.2, 0.9, 0.3, 1.15),
      visibility 0s;
    visibility: visible;
  }

  .pad-card.is-open {
    pointer-events: auto;
    transform: translateY(0) rotate(calc(var(--r) * 1deg));
    transition:
      transform 480ms cubic-bezier(0.3, 1.2, 0.4, 1),
      visibility 0s;
    visibility: visible;
  }

  .pad.is-swapping .pad-card.is-open {
    transition-delay: 100ms, 0s;
  }

  .pad-card--mirror {
    --peek-r: 1.8deg;
    transform-origin: 0 0;
  }

  .pad-foot {
    align-items: center;
    display: flex;
    flex-wrap: wrap;
    gap: 0 16px;
    justify-content: space-between;
  }

  .pad-foot--end {
    justify-content: flex-end;
  }

  .pad-card [data-row] {
    opacity: 0;
    transform: translateY(-5px);
    transition:
      opacity 120ms,
      transform 120ms;
  }

  .pad-card.is-open [data-row] {
    opacity: 1;
    transform: none;
    transition:
      opacity 240ms ease-out,
      transform 240ms ease-out;
    transition-delay: calc(120ms + var(--i, 0) * 20ms);
  }

  @media (max-width: 767.98px) {
    .pad-card {
      --peek: 24px;
      --peek-r: 2.4deg;
      left: 50px;
      right: 50px;
      transform-origin: 0 0;
      width: auto;
    }

    .pad-card--desk {
      display: none;
    }

    .pad-card--phone .kcc-sheet {
      background-attachment: local;
      max-height: min(520px, calc(100dvh - 128px));
      overflow-y: auto;
      overscroll-behavior: contain;
      scroll-padding-block: var(--bl);
      scrollbar-width: none;
    }
  }

  @media (min-width: 768px) {
    .pad-card--phone {
      display: none;
    }
  }

  @media (prefers-reduced-motion: reduce) {
    .pad-card,
    .pad-card [data-row] {
      transition-delay: 0s !important;
    }
  }
</style>
