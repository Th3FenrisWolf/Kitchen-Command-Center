<script setup lang="ts">
  import { computed } from 'vue'
  import { starStates } from '~/Components/StarRating/starDisplay'

  const props = withDefaults(
    defineProps<{
      modelValue: number
      readonly?: boolean
      max?: number
    }>(),
    { readonly: false, max: 5 },
  )

  const emit = defineEmits<{ 'update:modelValue': [value: number] }>()

  const states = computed(() => starStates(props.modelValue, props.max))
  const rounded = computed(() => Math.round(props.modelValue))

  const iconFor = (state: 'full' | 'half' | 'empty') =>
    state === 'full' ? 'fa-solid fa-star' : state === 'half' ? 'fa-solid fa-star-half-stroke' : 'fa-regular fa-star'

  const pick = (star: number) => {
    if (!props.readonly) emit('update:modelValue', star)
  }
</script>

<template>
  <div class="inline-flex items-center gap-0.5" role="img" :aria-label="`${rounded} of ${max} stars`">
    <template v-for="(state, i) in states" :key="i">
      <button
        v-if="!readonly"
        type="button"
        class="cursor-pointer p-0.5 text-peach"
        :aria-label="`${i + 1} star${i === 0 ? '' : 's'}`"
        :data-star="i + 1"
        :data-state="state"
        @click="pick(i + 1)"
      >
        <i :class="iconFor(state)" aria-hidden="true"></i>
      </button>
      <span v-else class="text-peach" :data-star="i + 1" :data-state="state">
        <i :class="iconFor(state)" aria-hidden="true"></i>
      </span>
    </template>
  </div>
</template>
