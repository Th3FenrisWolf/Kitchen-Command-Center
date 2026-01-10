<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Recipe } from '~/types/recipe'

const { id } = defineProps<{
  id: string
}>()

const recipe = ref<Recipe | null>(null)

onMounted(async () => {
  //recipe.value = await getRecipe(id)
})
</script>

<template>
  <div>Welcome to Recipe Detail: {{ id }}</div>

  <div>
    <p>{{ recipe?.title }}</p>
    <p>{{ recipe?.description }}</p>
    <h3>Ingredients:</h3>
    <ul>
      <li v-for="(ingredient, index) in recipe?.ingredients ?? []" :key="index">
        {{ ingredient.quantity }} {{ ingredient.unit }} {{ ingredient.name }}
        <span v-if="ingredient.isEyeballed"> (Eyeballed)</span>
      </li>
    </ul>
    <h3>Instructions:</h3>
    <ol>
      <li v-for="(instruction, index) in recipe?.instructions ?? []" :key="index">
        {{ index + 1 }}. {{ instruction.text }}
      </li>
    </ol>
  </div>
</template>
