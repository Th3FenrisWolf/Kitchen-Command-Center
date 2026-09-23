<!-- #region VariantIngredients Component Properties -->
<script lang="ts">
  import { computed, ref } from 'vue'
  import type { Ingredient } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { formatIngredientAmount } from './variantScaling'

  /**
   * Checklist of ingredients whose amounts rescale with the chosen serving count.
   */
  export default {
    name: 'VariantIngredients',
  }

  export interface VariantIngredientsProps {
    ingredients: Ingredient[]
    /**
     * Servings the stored amounts were written for. Omit or pass 0 to hide the scaler.
     */
    baseServings?: number
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantIngredientsProps>()

  const rs = useResourceStrings()
  const hasScaler = computed(() => (props.baseServings ?? 0) > 0)
  const current = ref(props.baseServings && props.baseServings > 0 ? props.baseServings : 1)
  const checked = ref<Record<number, boolean>>({})

  const amounts = computed(() =>
    props.ingredients.map((ingredient) => formatIngredientAmount(ingredient, props.baseServings ?? 0, current.value)),
  )

  const dec = () => {
    current.value = Math.max(1, current.value - 1)
  }
  const inc = () => {
    current.value = current.value + 1
  }
  const toggle = (i: number) => {
    checked.value = { ...checked.value, [i]: !checked.value[i] }
  }
</script>

<template>
  <div class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h2 class="font-casual text-2xl tracking-[1px]"><ResourceString for="Ingredients" /></h2>
      <div v-if="hasScaler" class="flex items-center gap-2.5">
        <span class="text-sm font-bold text-ink-soft"><ResourceString for="Makes" /></span>
        <button
          type="button"
          :aria-label="rs('Fewer')"
          class="sk-btn sk-btn--ghost size-8.5 text-sm"
          v-ink="'button'"
          @click="dec"
        >
          <i class="fa-solid fa-minus"></i>
        </button>
        <span class="min-w-14.5 text-center font-casual text-xl leading-none">{{ current }}</span>
        <button
          type="button"
          :aria-label="rs('More')"
          class="sk-btn sk-btn--ghost size-8.5 text-sm"
          v-ink="'button'"
          @click="inc"
        >
          <i class="fa-solid fa-plus"></i>
        </button>
      </div>
    </div>

    <div class="rounded-3xl bg-paper-2 p-6">
      <ul class="flex flex-col">
        <li
          v-for="(ingredient, i) in ingredients"
          :key="i"
          class="flex cursor-pointer items-start gap-3 border-b border-rule py-2.5 last:border-b-0"
          @click="toggle(i)"
        >
          <span
            class="mt-px grid size-5.5 flex-none place-items-center rounded-[7px] border-2 transition-all"
            :class="checked[i] ? 'border-marker bg-marker' : 'border-ink-soft bg-transparent'"
          >
            <i v-if="checked[i]" class="fa-solid fa-check text-[11px] text-marker-ink"></i>
          </span>
          <span
            class="text-base leading-snug transition-all"
            :class="checked[i] ? 'text-ink-soft line-through opacity-60' : 'text-ink'"
          >
            <b v-if="amounts[i]">{{ amounts[i] }}</b> {{ ingredient.name }}
            <span v-if="ingredient.isEyeballed" class="text-ink-soft italic"> — <ResourceString for="ToTaste" /></span>
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>
