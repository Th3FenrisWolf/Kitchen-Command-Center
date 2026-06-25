<script setup lang="ts">
  import { ref } from 'vue'
  import type { Ingredient } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import { formatIngredientAmount } from './variantScaling'

  const props = defineProps<{ ingredients: Ingredient[]; baseServings?: number }>()

  const hasScaler = (props.baseServings ?? 0) > 0
  const current = ref(hasScaler ? (props.baseServings as number) : 1)
  const checked = ref<Record<number, boolean>>({})

  const dec = () => {
    current.value = Math.max(1, current.value - 1)
  }
  const inc = () => {
    current.value = current.value + 1
  }
  const toggle = (i: number) => {
    checked.value = { ...checked.value, [i]: !checked.value[i] }
  }
  const amount = (ingredient: Ingredient) =>
    formatIngredientAmount(ingredient, props.baseServings ?? 0, current.value)
</script>

<template>
  <div class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h2 class="font-casual text-2xl tracking-[1px]"><ResourceString for="Ingredients" /></h2>
      <div v-if="hasScaler" class="flex items-center gap-2.5">
        <span class="text-sm font-bold text-onyx-light"><ResourceString for="Makes" /></span>
        <button
          type="button"
          aria-label="Fewer"
          class="grid h-[34px] w-[34px] cursor-pointer place-items-center rounded-full border-none bg-surface-500 text-sm text-bone transition-colors hover:bg-surface-400"
          @click="dec"
        >
          <i class="fa-solid fa-minus"></i>
        </button>
        <span class="min-w-[58px] text-center font-casual text-xl leading-none">{{ current }}</span>
        <button
          type="button"
          aria-label="More"
          class="grid h-[34px] w-[34px] cursor-pointer place-items-center rounded-full border-none bg-surface-500 text-sm text-bone transition-colors hover:bg-surface-400"
          @click="inc"
        >
          <i class="fa-solid fa-plus"></i>
        </button>
      </div>
    </div>

    <div class="rounded-3xl bg-bone p-6 shadow-primary">
      <ul class="flex flex-col">
        <li
          v-for="(ingredient, i) in ingredients"
          :key="i"
          class="flex cursor-pointer items-start gap-3 border-b border-bone-dark py-2.5 last:border-b-0"
          @click="toggle(i)"
        >
          <span
            class="mt-px grid h-[22px] w-[22px] flex-none place-items-center rounded-[7px] border-2 transition-all"
            :class="checked[i] ? 'border-surface-500 bg-surface-500' : 'border-overlay-300 bg-transparent'"
          >
            <i v-if="checked[i]" class="fa-solid fa-check text-[11px] text-bone"></i>
          </span>
          <span
            class="text-base leading-snug transition-all"
            :class="checked[i] ? 'text-onyx-light line-through opacity-60' : 'text-onyx'"
          >
            <b v-if="amount(ingredient)">{{ amount(ingredient) }} </b>{{ ingredient.name }}<span
              v-if="ingredient.isEyeballed"
              class="italic text-onyx-light"
            >
              — <ResourceString for="ToTaste" /></span>
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>
