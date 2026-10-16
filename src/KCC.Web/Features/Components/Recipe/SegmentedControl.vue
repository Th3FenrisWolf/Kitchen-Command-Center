<script setup lang="ts" generic="T extends string">
  import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

  export interface SegmentOption<V extends string = string> {
    value: V
    label?: string
    icon?: string
    ariaLabel?: string
    title?: string
    testId?: string
  }

  export interface SegmentedControlProps<T extends string> {
    options: SegmentOption<T>[]
    ariaLabel?: string
    variant?: 'text' | 'icon'
  }

  const { options, ariaLabel, variant = 'text' } = defineProps<SegmentedControlProps<T>>()

  const model = defineModel<T>({ required: true })

  const track = ref<HTMLElement>()
  const thumbEl = ref<HTMLElement>()
  const thumb = ref<Record<string, string>>({})
  const mounted = ref(false)
  const glide = ref(false)
  let resizes: ResizeObserver | undefined

  const checkedIndex = computed(() => options.findIndex((o) => o.value === model.value))
  const sliding = computed(() => mounted.value && checkedIndex.value >= 0)

  const buttons = () => (track.value ? Array.from(track.value.querySelectorAll<HTMLElement>('[data-seg]')) : [])

  // offsetLeft rather than a getBoundingClientRect difference: the track scrolls when narrow, and the thumb
  // scrolls with the buttons.
  function place(slide: boolean) {
    const button = buttons()[checkedIndex.value]
    if (!button) {
      return
    }
    thumb.value = { width: `${button.offsetWidth}px`, transform: `translateX(${button.offsetLeft}px)` }
    if (!slide) {
      glide.value = false
      // Reading the layout makes the browser take the new place with no transition before glide turns it on.
      nextTick(() => {
        thumbEl.value?.getBoundingClientRect()
        glide.value = true
      })
    }
  }

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

  watch(model, () => nextTick(() => place(true)))
  watch(
    () => options.map((o) => o.label ?? o.icon).join('|'),
    () => nextTick(() => place(false)),
  )

  onMounted(() => {
    place(false)
    mounted.value = true
    if (typeof ResizeObserver !== 'undefined' && track.value) {
      resizes = new ResizeObserver(() => place(false))
      resizes.observe(track.value)
    }
    document.fonts?.ready?.then(() => place(false))
  })

  onBeforeUnmount(() => resizes?.disconnect())
</script>

<template>
  <div ref="track" role="radiogroup" :aria-label="ariaLabel" class="kcc-seg" :class="{ 'kcc-seg--sliding': sliding }">
    <span
      ref="thumbEl"
      class="kcc-seg-thumb"
      :class="{ 'kcc-seg-thumb--glide': glide }"
      :style="thumb"
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
      :class="{ 'w-9 px-0': variant === 'icon' }"
      @click="model = opt.value"
      @keydown="onKeydown($event, i)"
    >
      <i v-if="opt.icon" :class="opt.icon" aria-hidden="true"></i>
      <span v-if="opt.label">{{ opt.label }}</span>
    </button>
  </div>
</template>
