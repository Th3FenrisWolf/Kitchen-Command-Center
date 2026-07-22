<script lang="ts">
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
</script>

<script setup lang="ts" generic="T extends string">
  import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

  const props = withDefaults(
    defineProps<{
      options: SegmentOption<T>[]
      /** Accessible group name announced for the radiogroup. */
      ariaLabel?: string
      /** `text` = padded label pills; `icon` = fixed-width icon squares. */
      variant?: 'text' | 'icon'
    }>(),
    { variant: 'text' },
  )

  const model = defineModel<T>({ required: true })

  const track = ref<HTMLElement>()
  const thumb = ref<Record<string, string>>({})
  // `ready` flips on client mount, handing the highlight from the active button's own
  // background over to the sliding thumb. Until then (SSR + no-JS) the button carries it.
  const ready = ref(false)
  // Off for the first placement so the thumb doesn't slide in from the left on load.
  const animate = ref(false)
  let ro: ResizeObserver | undefined

  const activeIndex = computed(() => props.options.findIndex((o) => o.value === model.value))
  const showThumb = computed(() => ready.value && activeIndex.value >= 0)

  const buttons = () => (track.value ? Array.from(track.value.querySelectorAll<HTMLElement>('[data-seg]')) : [])

  /** Position the thumb over the active button. `withAnimation` false = snap without a transition. */
  function place(withAnimation: boolean) {
    const el = buttons()[activeIndex.value]
    if (!track.value || !el) {
      return
    }
    const t = track.value.getBoundingClientRect()
    const b = el.getBoundingClientRect()
    const next = {
      width: `${b.width}px`,
      height: `${b.height}px`,
      transform: `translate(${b.left - t.left}px, ${b.top - t.top}px)`,
    }
    if (withAnimation) {
      thumb.value = next
      return
    }
    animate.value = false
    thumb.value = next
    nextTick(() => requestAnimationFrame(() => (animate.value = true)))
  }

  function onKeydown(e: KeyboardEvent, i: number) {
    const n = props.options.length
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
    model.value = props.options[next].value
    nextTick(() => buttons()[next]?.focus())
  }

  watch(model, () => place(true))
  // Options can change width (e.g. a relabel); re-snap without animating.
  watch(
    () => props.options.map((o) => o.label ?? o.icon).join('|'),
    () => place(false),
  )

  onMounted(() => {
    place(false)
    ready.value = true
    // Re-snap on any later reflow. With the site-wide font gate (Layout.cshtml) fonts are
    // already loaded before paint, but this keeps the thumb correct through container
    // resizes, zoom, or a late layout shift if the gate ever times out.
    if (typeof ResizeObserver !== 'undefined' && track.value) {
      ro = new ResizeObserver(() => place(false))
      ro.observe(track.value)
    }
    document.fonts?.ready?.then(() => place(false))
  })

  onBeforeUnmount(() => ro?.disconnect())
</script>

<template>
  <div
    ref="track"
    role="radiogroup"
    :aria-label="ariaLabel"
    class="relative inline-flex items-center rounded-2xl bg-bone-dark p-1"
  >
    <span
      class="seg-thumb pointer-events-none absolute top-0 left-0 rounded-xl bg-surface-500"
      :class="{ 'is-animated': animate }"
      :style="[thumb, { opacity: showThumb ? 1 : 0 }]"
      aria-hidden="true"
    ></span>

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
      class="relative z-1 flex cursor-pointer items-center justify-center border-none font-bold whitespace-nowrap transition-colors outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-onyx"
      :class="[
        variant === 'icon' ? 'h-8 w-10 rounded-xl text-[15px]' : 'rounded-xl px-4 py-2 text-sm',
        opt.value === model ? (ready ? 'text-bone' : 'bg-surface-500 text-bone') : 'text-onyx-light hover:text-onyx',
      ]"
      @click="model = opt.value"
      @keydown="onKeydown($event, i)"
    >
      <i v-if="opt.icon" :class="opt.icon"></i>
      <span v-if="opt.label">{{ opt.label }}</span>
    </button>
  </div>
</template>

<style scoped>
  .seg-thumb {
    transition-duration: 260ms;
    /* No transition for the initial placement; enabled via `.is-animated` after mount. */
    transition-property: none;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  }
  .seg-thumb.is-animated {
    transition-property: transform, width, height;
  }
  @media (prefers-reduced-motion: reduce) {
    .seg-thumb.is-animated {
      transition-property: none;
    }
  }
</style>
