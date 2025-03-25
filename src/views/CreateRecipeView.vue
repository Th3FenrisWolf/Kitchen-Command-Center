<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { storeToRefs } from 'pinia'
import { ref } from 'vue'
import InputField from '~/components/shared/InputField.vue'
import TextAreaField from '~/components/shared/TextAreaField.vue'
import SmallHero from '~/components/SmallHero.vue'
import { addRecipe } from '~/database/recipes'
import { useUserStore } from '~/store/user'

interface Ingredient {
  name: string
  quantity: number
  unit: string
}

const userStore = useUserStore()
const { user } = storeToRefs(userStore)

const title = ref('')
const description = ref('')
const ingredients = ref('')
const ingredientList = ref<Ingredient[]>([{ name: '', quantity: NaN, unit: '' }])

const handleSubmit = () => {
  if (!user.value) {
    console.error('User is not logged in')
    return
  }

  addRecipe(title.value, description.value, ingredients.value, user.value.uid)
  title.value = ''
  description.value = ''
  ingredients.value = ''
  console.log('Recipe added successfully!')
}
</script>

<template>
  <SmallHero dark>
    <template #title>Create Recipe</template>
  </SmallHero>

  <section>
    <form @submit.prevent="handleSubmit" class="flex flex-col gap-4">
      <fieldset class="flex flex-col gap-2">
        <legend class="text-2xl font-bold">Recipe Details</legend>

        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Title</span>
          <InputField v-model="title" required type="text" placeholder="Enter recipe title" />
        </label>

        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Description</span>
          <TextAreaField v-model="description" required placeholder="Enter recipe description" />
        </label>
      </fieldset>

      <fieldset>
        <legend class="mb-2 text-2xl font-bold">Ingredients</legend>

        <div class="mb-2 flex gap-2">
          <label id="ingredient-name" class="shrink grow basis-1/4">
            <span class="text-lg font-bold">Ingredient Name</span>
          </label>

          <label id="ingredient-quantity" class="shrink basis-1/4">
            <span class="text-lg font-bold">Quantity</span>
          </label>

          <label id="quantity-unit" class="shrink basis-1/4">
            <span class="text-lg font-bold">Unit</span>
          </label>

          <span class="max-w-12 basis-1/12" />
        </div>

        <div class="flex flex-col gap-4">
          <div class="flex gap-2" v-for="(ingredient, index) in ingredientList" :key="index">
            <InputField
              class="shrink grow basis-1/4"
              v-model="ingredient.name"
              aria-labelledby="ingredient-name"
              title="Enter ingredient name"
              required
              type="text"
            />

            <InputField
              class="shrink basis-1/4"
              v-model="ingredient.quantity"
              aria-labelledby="ingredient-quantity"
              title="Enter ingredient quantity"
              required
              type="number"
            />

            <InputField
              class="shrink basis-1/4"
              v-model="ingredient.unit"
              aria-labelledby="quantity-unit"
              title="Enter quantity unit"
              required
              type="text"
            />

            <button
              aria-label="Remove ingredient"
              type="button"
              :disabled="ingredientList.length <= 1"
              class="bg-base disabled:bg-overlay-100 ease-normal h-12 max-w-12 basis-1/12 cursor-pointer self-end rounded-2xl p-2 text-white transition-colors duration-300 disabled:cursor-not-allowed"
              @click="ingredientList.splice(index, 1)"
            >
              <FontAwesomeIcon :icon="['fas', 'trash']" />
            </button>
          </div>
        </div>

        <button
          type="button"
          class="bg-base text-bone mt-4 cursor-pointer rounded-3xl px-4 py-2 text-xl"
          @click="ingredientList.push({ name: '', quantity: NaN, unit: '' })"
        >
          Add Ingredient
        </button>
      </fieldset>

      <button type="submit" class="bg-base text-bone cursor-pointer rounded-3xl px-4 py-2 text-xl">
        Create Recipe
      </button>
    </form>
  </section>
</template>
