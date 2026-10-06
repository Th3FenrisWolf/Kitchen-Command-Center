<script setup lang="ts">
  import type { NavRecipes } from '~/Types/Nav'
  import PadLines from './PadLines.vue'
  import type { NavLabel } from './navLabels'

  export interface RecipesCardProps {
    recipes: NavRecipes
    total: number
    library?: string
    currentPage: string
    t: NavLabel
  }

  const { recipes, total, library, currentPage, t } = defineProps<RecipesCardProps>()
</script>

<template>
  <div class="pad-columns">
    <div v-if="recipes.meals.length">
      <p class="kcc-kick" data-row style="--i: 0">{{ t('Meals') }}</p>
      <PadLines :rows="recipes.meals" :from="1" glyphs :current-page />
    </div>
    <div v-if="recipes.diets.length">
      <p class="kcc-kick" data-row style="--i: 0">{{ t('Diets') }}</p>
      <PadLines :rows="recipes.diets" :from="1" :current-page />
    </div>
    <div v-if="recipes.quickPicks.length">
      <p class="kcc-kick" data-row style="--i: 0">{{ t('QuickPicks') }}</p>
      <PadLines :rows="recipes.quickPicks" :from="1" glyphs :current-page />
    </div>
  </div>

  <template v-if="library || recipes.note">
    <hr class="kcc-hr" data-row style="--i: 7" />
    <div class="pad-foot" data-row style="--i: 7">
      <p v-if="library" class="kcc-kick">
        <a class="kcc-link" :href="library">{{ t('AllRecipes', total) }}</a>
        <i class="fa-solid fa-arrow-right ml-2" aria-hidden="true"></i>
      </p>
      <p v-if="recipes.note" class="kcc-hand">{{ recipes.note }}</p>
    </div>
  </template>
</template>

<style>
  .pad-columns {
    display: grid;
    gap: 0 24px;
    grid-auto-columns: minmax(0, 1fr);
    grid-auto-flow: column;
  }
</style>
