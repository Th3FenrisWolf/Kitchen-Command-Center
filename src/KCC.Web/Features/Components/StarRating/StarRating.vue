<script setup lang="ts">
  import { computed, ref } from 'vue'
  import { formatRating, starStates } from '~/Components/StarRating/starDisplay'
  import { MIN_RATING, stepRating } from '~/Components/StarRating/ratingInput'

  const props = withDefaults(
    defineProps<{
      modelValue: number
      readonly?: boolean
      max?: number
    }>(),
    { readonly: false, max: 5 },
  )

  const emit = defineEmits<{ 'update:modelValue': [value: number] }>()

  // Live pointer preview; falls back to the committed value.
  const hoverValue = ref<number | null>(null)
  const displayValue = computed(() => hoverValue.value ?? props.modelValue)
  const states = computed(() => starStates(displayValue.value, props.max))

  const iconFor = (state: 'full' | 'half' | 'empty') =>
    state === 'full' ? 'fa-solid fa-star' : state === 'half' ? 'fa-solid fa-star-half-stroke' : 'fa-regular fa-star'

  const pick = (value: number) => emit('update:modelValue', value)
  const preview = (value: number) => (hoverValue.value = value)
  const clearPreview = () => (hoverValue.value = null)

  const onKeydown = (e: KeyboardEvent) => {
    if (e.key === 'ArrowRight' || e.key === 'ArrowUp') {
      e.preventDefault()
      pick(stepRating(props.modelValue, 0.5, props.max))
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowDown') {
      e.preventDefault()
      pick(stepRating(props.modelValue, -0.5, props.max))
    } else if (e.key === 'Home') {
      e.preventDefault()
      pick(MIN_RATING)
    } else if (e.key === 'End') {
      e.preventDefault()
      pick(props.max)
    }
  }
</script>

<template>
  <!-- Readonly: static display (already supports halves). -->
  <div
    v-if="readonly"
    class="inline-flex items-center gap-0.5"
    role="img"
    :aria-label="`${formatRating(modelValue)} of ${max} stars`"
  >
    <span v-for="(state, i) in states" :key="i" class="text-peach" :data-star="i + 1" :data-state="state">
      <i :class="iconFor(state)" aria-hidden="true"></i>
    </span>
  </div>

  <!-- Interactive: half-selectable slider. Keyboard drives the slider; the two
       pointer-only hit areas per star let a mouse/touch pick a half or whole. -->
  <div
    v-else
    class="inline-flex items-center gap-0.5 text-peach"
    role="slider"
    tabindex="0"
    aria-label="Rating"
    :aria-valuemin="MIN_RATING"
    :aria-valuemax="max"
    :aria-valuenow="modelValue"
    :aria-valuetext="`${modelValue} of ${max} stars`"
    @keydown="onKeydown"
    @mouseleave="clearPreview"
  >
    <span
      v-for="(state, i) in states"
      :key="i"
      class="relative inline-block cursor-pointer p-0.5"
      :data-star="i + 1"
      :data-state="state"
    >
      <i :class="iconFor(state)" aria-hidden="true"></i>
      <span
        class="absolute inset-y-0 left-0 z-10 w-1/2"
        aria-hidden="true"
        :data-value="i + 0.5"
        @mouseenter="preview(i + 0.5)"
        @click="pick(i + 0.5)"
      ></span>
      <span
        class="absolute inset-y-0 right-0 z-10 w-1/2"
        aria-hidden="true"
        :data-value="i + 1"
        @mouseenter="preview(i + 1)"
        @click="pick(i + 1)"
      ></span>
    </span>
  </div>
</template>
