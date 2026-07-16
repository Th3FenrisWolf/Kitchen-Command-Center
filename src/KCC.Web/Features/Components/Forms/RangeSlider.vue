<script setup lang="ts">
  import { computed } from 'vue'

  const props = defineProps<{ min: number; max: number; step?: number }>()
  const modelMin = defineModel<number>('modelMin', { required: true })
  const modelMax = defineModel<number>('modelMax', { required: true })

  const pct = (v: number) => ((v - props.min) / (props.max - props.min)) * 100
  const fillStyle = computed(() => `left: ${pct(modelMin.value)}%; right: ${100 - pct(modelMax.value)}%`)

  const onMin = (e: Event) => {
    modelMin.value = Math.min(Number((e.target as HTMLInputElement).value), modelMax.value)
  }
  const onMax = (e: Event) => {
    modelMax.value = Math.max(Number((e.target as HTMLInputElement).value), modelMin.value)
  }
</script>

<template>
  <div class="relative mx-0.5 h-5">
    <div class="absolute inset-x-0 top-2 h-[5px] rounded-full bg-bone-dark"></div>
    <div class="absolute top-2 h-[5px] rounded-full bg-onyx" :style="fillStyle"></div>
    <input class="kcc-range" type="range" :min="min" :max="max" :step="step ?? 1" :value="modelMin" aria-label="Minimum time" @input="onMin" />
    <input class="kcc-range" type="range" :min="min" :max="max" :step="step ?? 1" :value="modelMax" aria-label="Maximum time" @input="onMax" />
  </div>
</template>

<style scoped>
  .kcc-range {
    -webkit-appearance: none;
    appearance: none;
    position: absolute;
    left: 0;
    top: 0;
    width: 100%;
    height: 20px;
    margin: 0;
    background: transparent;
    pointer-events: none;
  }
  .kcc-range:focus {
    outline: none;
  }
  .kcc-range::-webkit-slider-thumb {
    -webkit-appearance: none;
    appearance: none;
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: var(--onyx);
    border: 3px solid var(--bone);
    box-shadow: 0 0 0 1px var(--onyx);
    cursor: grab;
    pointer-events: auto;
  }
  .kcc-range::-moz-range-thumb {
    width: 20px;
    height: 20px;
    border-radius: 50%;
    background: var(--onyx);
    border: 3px solid var(--bone);
    box-shadow: 0 0 0 1px var(--onyx);
    cursor: grab;
    pointer-events: auto;
  }
</style>
