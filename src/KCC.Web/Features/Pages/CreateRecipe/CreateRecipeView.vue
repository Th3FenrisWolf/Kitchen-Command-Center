<script setup lang="ts">
  import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
  import { faTrash, faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons'
  import { ref } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.vue'
  import type { Ingredient, Instruction, Recipe } from '~/Types/Recipe'

  const { userId } = defineProps<{ userId: string }>()

  const title = ref('')
  const description = ref('')
  const ingredientList = ref<Ingredient[]>([{ name: '', unit: '', isEyeballed: false }])
  const instructionList = ref<Instruction[]>([{ text: '' }])

  const handleSubmit = () => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const recipe: Recipe = {
      title: title.value.trim(),
      description: description.value.trim(),
      ingredients: ingredientList.value,
      instructions: instructionList.value,
      createdBy: userId,
      createdOn: new Date(),
    }

    // addRecipe(recipe)
    title.value = ''
    description.value = ''
    ingredientList.value = [{ name: '', unit: '', isEyeballed: false }]
    instructionList.value = [{ text: '' }]
  }
</script>

<template>
  <SmallHero dark>
    <template #title>Create Recipe</template>
  </SmallHero>

  <section>
    <form @submit.prevent="handleSubmit" class="flex flex-col gap-8">
      <fieldset class="flex flex-col gap-2">
        <legend class="mb-2 text-2xl font-bold">Recipe Details</legend>

        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Title</span>
          <InputField v-model="title" required type="text" />
        </label>

        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Description</span>
          <TextAreaField v-model="description" required />
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

            <button
              aria-label="Eyeball Ingredient"
              type="button"
              class="ease-normal h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-base p-2 text-white disabled:bg-overlay-100"
              @click="ingredient.isEyeballed = !ingredient.isEyeballed"
            >
              <FontAwesomeIcon :icon="ingredient.isEyeballed ? faEye : faEyeSlash" />
            </button>

            <InputField
              class="shrink basis-1/4"
              v-model="ingredient.quantity"
              aria-labelledby="ingredient-quantity"
              title="Enter ingredient quantity"
              required
              type="number"
              step="0.01"
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
              class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-base p-2 text-white transition-colors duration-300 disabled:cursor-not-allowed disabled:bg-overlay-100"
              @click="ingredientList.splice(index, 1)"
            >
              <FontAwesomeIcon :icon="faTrash" />
            </button>
          </div>
        </div>

        <button
          type="button"
          class="mt-4 cursor-pointer rounded-3xl bg-base px-4 py-2 text-xl text-bone"
          @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })"
        >
          Add Ingredient
        </button>
      </fieldset>

      <fieldset>
        <legend class="mb-2 text-2xl font-bold">Instructions</legend>

        <div class="mb-2 flex gap-4">
          <label id="step-number" class="max-w-12 basis-1/12">
            <span class="text-lg font-bold">Step #</span>
          </label>

          <label id="step-description" class="shrink grow basis-11/12">
            <span class="text-lg font-bold">Description</span>
          </label>
        </div>

        <div class="flex flex-col gap-4">
          <div class="flex gap-4" v-for="(instruction, index) in instructionList" :key="index">
            <p class="max-w-12 basis-1/12 pt-1.5 text-end text-2xl">{{ index + 1 }}.</p>

            <TextAreaField
              class="shrink grow basis-10/12"
              v-model="instruction.text"
              aria-labelledby="instructions"
              title="Enter instruction text"
              required
            />

            <button
              aria-label="Remove step"
              type="button"
              :disabled="instructionList.length <= 1"
              class="h-12 max-w-12 basis-1/12 cursor-pointer self-center rounded-2xl bg-base p-2 text-white transition-colors duration-300 disabled:cursor-not-allowed disabled:bg-overlay-100"
              @click="instructionList.splice(index, 1)"
            >
              <FontAwesomeIcon :icon="faTrash" />
            </button>
          </div>
        </div>

        <button
          type="button"
          class="mt-4 cursor-pointer rounded-3xl bg-base px-4 py-2 text-xl text-bone"
          @click="instructionList.push({ step: NaN, text: '' })"
        >
          Add Instruction
        </button>
      </fieldset>

      <button type="submit" class="cursor-pointer rounded-3xl bg-base px-4 py-2 text-xl text-bone">Create Recipe</button>
    </form>
  </section>
</template>
