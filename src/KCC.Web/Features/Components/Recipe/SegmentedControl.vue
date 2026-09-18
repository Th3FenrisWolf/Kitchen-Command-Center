<!-- #region SegmentedControl Component Properties -->
<script lang="ts">
  import { nextTick, ref } from 'vue'

  /**
   * Radiogroup of pills; the active segment is a solid ink fill (`kcc-seg`).
   */
  export default {
    name: 'SegmentedControl',
  }

  /** One selectable segment. Provide `label` for text pills or `icon` for icon-only toggles. */
  export interface SegmentOption<V extends string = string> {
    /** Value bound to the control's model when this segment is active. */
    value: V
    /** Visible, already-localized text (text segments). */
    label?: string
    /** Font Awesome classes for an icon-only segment, e.g. 'fa-solid fa-list'. */
    icon?: string
    /** Accessible name — required for icon-only segments (no visible `label`). */
    ariaLabel?: string
    /** Native tooltip text. */
    title?: string
    /** Optional `data-testid` hook. */
    testId?: string
  }

  export interface SegmentedControlProps<T extends string> {
    options: SegmentOption<T>[]
    /**
     * Accessible group name announced for the radiogroup.
     */
    ariaLabel?: string
    /**
     * `text` = padded label pills; `icon` = fixed-width icon squares.
     * @default 'text'
     */
    variant?: 'text' | 'icon'
  }
</script>
<!-- #endregion -->

<script setup lang="ts" generic="T extends string">
  const { options, ariaLabel, variant = 'text' } = defineProps<SegmentedControlProps<T>>()

  const model = defineModel<T>({ required: true })

  const track = ref<HTMLElement>()

  const buttons = () => (track.value ? Array.from(track.value.querySelectorAll<HTMLElement>('[data-seg]')) : [])

  function onKeydown(e: KeyboardEvent, i: number) {
    const n = options.length
    const moves: Record<string, number> = {
      ArrowRight: (i + 1) % n,
      ArrowDown: (i + 1) % n,
      ArrowLeft: (i - 1 + n) % n,
      ArrowUp: (i - 1 + n) % n,
      Home: 0,
      End: n - 1,
    }
    const next = moves[e.key]
    if (next === undefined) {
      return
    }
    e.preventDefault()
    const target = options[next]
    if (!target) {
      return
    }
    model.value = target.value
    nextTick(() => buttons()[next]?.focus())
  }
</script>

<template>
  <div ref="track" role="radiogroup" :aria-label="ariaLabel" class="kcc-seg">
    <button
      v-for="(opt, i) in options"
      :key="opt.value"
      data-seg
      type="button"
      role="radio"
      :aria-checked="opt.value === model"
      :aria-label="opt.ariaLabel"
      :title="opt.title"
      :data-testid="opt.testId"
      :tabindex="opt.value === model ? 0 : -1"
      :class="{ 'w-9 px-0': variant === 'icon' }"
      @click="model = opt.value"
      @keydown="onKeydown($event, i)"
    >
      <i v-if="opt.icon" :class="opt.icon" aria-hidden="true"></i>
      <span v-if="opt.label">{{ opt.label }}</span>
    </button>
  </div>
</template>
