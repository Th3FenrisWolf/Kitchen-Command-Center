<script setup lang="ts">
  import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
  import { faTrash, faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons'
  import { ref, computed } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'

  const props = defineProps<{
    antiforgeryToken?: string
    resourceStrings?: Record<string, string>
  }>()

  useResourceStrings(props.resourceStrings, 'CreateRecipe')

  const step = ref(1)
  const totalSteps = 5
  const isSubmitting = ref(false)
  const submitError = ref('')
  const submitSuccess = ref(false)

  // Step 1: Recipe basics
  const recipeName = ref('')
  const recipeDescription = ref('')

  // Step 2: Variant info
  const variantName = ref('')
  const variantDescription = ref('')
  const prepTime = ref<number | undefined>()
  const cookTime = ref<number | undefined>()
  const servings = ref<number | undefined>()

  // Step 3: Ingredients
  const ingredientList = ref<Ingredient[]>([{ name: '', unit: '', isEyeballed: false }])

  // Step 4: Instructions
  const instructionList = ref<Instruction[]>([{ text: '' }])

  /* eslint-disable vue/script-indent */
  const canProceed = computed(() => {
    switch (step.value) {
      case 1:
        return recipeName.value.trim() !== '' && recipeDescription.value.trim() !== ''
      case 2:
        return variantName.value.trim() !== ''
      case 3:
        return ingredientList.value.some((i) => i.name.trim() !== '')
      case 4:
        return instructionList.value.some((i) => i.text.trim() !== '')
      default:
        return true
    }
  })
  /* eslint-enable vue/script-indent */

  const handleSubmit = async () => {
    isSubmitting.value = true
    submitError.value = ''

    try {
      const response = await fetch('/api/recipes', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(props.antiforgeryToken ? { RequestVerificationToken: props.antiforgeryToken } : {}),
        },
        body: JSON.stringify({
          recipeName: recipeName.value.trim(),
          recipeDescription: recipeDescription.value.trim(),
          firstVariant: {
            variantName: variantName.value.trim(),
            variantDescription: variantDescription.value.trim(),
            prepTime: prepTime.value || null,
            cookTime: cookTime.value || null,
            servings: servings.value || null,
            ingredients: ingredientList.value.filter((i) => i.name.trim() !== ''),
            instructions: instructionList.value
              .filter((i) => i.text.trim() !== '')
              .map((i, idx) => ({ step: idx + 1, text: i.text.trim() })),
          },
        }),
      })

      if (!response.ok) {
        const data = await response.json()
        submitError.value = data.error || 'Failed to create recipe.'
        return
      }

      submitSuccess.value = true
    } catch {
      submitError.value = 'An unexpected error occurred.'
    } finally {
      isSubmitting.value = false
    }
  }
</script>

<template>
  <SmallHero dark>
    <template #title><ResourceString for="CreateRecipe" /></template>
  </SmallHero>

  <section v-if="submitSuccess" class="flex flex-col items-center gap-4 py-12 text-center">
    <h2 class="font-casual text-4xl">Recipe Submitted!</h2>
    <p class="text-lg">Your recipe has been submitted for review. An admin will review and publish it.</p>
    <a href="/recipes" class="rounded-3xl bg-surface-500 px-6 py-3 text-xl text-bone">Back to Recipes</a>
  </section>

  <section v-else>
    <!-- Progress bar -->
    <div class="mb-8 flex gap-2">
      <div
        v-for="s in totalSteps"
        :key="s"
        class="h-2 flex-1 rounded-full transition-colors duration-300"
        :class="s <= step ? 'bg-surface-500' : 'bg-overlay-300'"
      />
    </div>

    <!-- Step 1: Recipe Basics -->
    <form v-if="step === 1" @submit.prevent="step++" class="flex flex-col gap-6">
      <h2 class="text-2xl font-bold">Recipe Basics</h2>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Recipe Name</span>
        <InputField v-model="recipeName" required type="text" placeholder="e.g., Mac & Cheese" />
      </label>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <TextAreaField v-model="recipeDescription" required placeholder="A short description of this dish" />
      </label>
      <button
        type="submit"
        :disabled="!canProceed"
        class="cursor-pointer self-end rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
      >
        Next →
      </button>
    </form>

    <!-- Step 2: Variant Info -->
    <form v-if="step === 2" @submit.prevent="step++" class="flex flex-col gap-6">
      <h2 class="text-2xl font-bold">First Variant</h2>
      <p class="text-overlay-400">Each recipe needs at least one variant — a specific way to make it.</p>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Variant Name</span>
        <InputField v-model="variantName" required type="text" placeholder="e.g., Classic Stovetop" />
      </label>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <TextAreaField v-model="variantDescription" placeholder="What makes this variant special?" />
      </label>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Prep Time (min)</span>
          <InputField v-model="prepTime" type="number" />
        </label>
        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Cook Time (min)</span>
          <InputField v-model="cookTime" type="number" />
        </label>
        <label class="flex flex-col gap-2">
          <span class="text-lg font-bold">Servings</span>
          <InputField v-model="servings" type="number" />
        </label>
      </div>
      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          ← Back
        </button>
        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next →
        </button>
      </div>
    </form>

    <!-- Step 3: Ingredients -->
    <form v-if="step === 3" @submit.prevent="step++" class="flex flex-col gap-6">
      <h2 class="text-2xl font-bold">Ingredients</h2>
      <div class="flex flex-col gap-4">
        <div class="flex gap-2" v-for="(ingredient, index) in ingredientList" :key="index">
          <InputField class="shrink grow basis-1/3" v-model="ingredient.name" placeholder="Ingredient name" type="text" />
          <button
            type="button"
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white"
            @click="ingredient.isEyeballed = !ingredient.isEyeballed"
          >
            <FontAwesomeIcon :icon="ingredient.isEyeballed ? faEye : faEyeSlash" />
          </button>
          <InputField
            class="shrink basis-1/6"
            v-model="ingredient.quantity"
            placeholder="Qty"
            type="number"
            step="0.01"
            :disabled="ingredient.isEyeballed"
          />
          <InputField
            class="shrink basis-1/6"
            v-model="ingredient.unit"
            placeholder="Unit"
            type="text"
            :disabled="ingredient.isEyeballed"
          />
          <button
            type="button"
            :disabled="ingredientList.length <= 1"
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="ingredientList.splice(index, 1)"
          >
            <FontAwesomeIcon :icon="faTrash" />
          </button>
        </div>
      </div>
      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-surface-500 px-4 py-2 text-bone"
        @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })"
      >
        Add Ingredient
      </button>
      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          ← Back
        </button>
        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next →
        </button>
      </div>
    </form>

    <!-- Step 4: Instructions -->
    <form v-if="step === 4" @submit.prevent="step++" class="flex flex-col gap-6">
      <h2 class="text-2xl font-bold">Instructions</h2>
      <div class="flex flex-col gap-4">
        <div class="flex gap-4" v-for="(instruction, index) in instructionList" :key="index">
          <p class="max-w-12 pt-1.5 text-end text-2xl">{{ index + 1 }}.</p>
          <TextAreaField class="shrink grow" v-model="instruction.text" placeholder="Describe this step" />
          <button
            type="button"
            :disabled="instructionList.length <= 1"
            class="h-12 max-w-12 cursor-pointer self-center rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="instructionList.splice(index, 1)"
          >
            <FontAwesomeIcon :icon="faTrash" />
          </button>
        </div>
      </div>
      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-surface-500 px-4 py-2 text-bone"
        @click="instructionList.push({ text: '' })"
      >
        Add Step
      </button>
      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          ← Back
        </button>
        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          Next →
        </button>
      </div>
    </form>

    <!-- Step 5: Review & Submit -->
    <div v-if="step === 5" class="flex flex-col gap-6">
      <h2 class="text-2xl font-bold">Review & Submit</h2>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h3 class="font-casual text-3xl">{{ recipeName }}</h3>
        <p>{{ recipeDescription }}</p>
      </div>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h3 class="text-xl font-bold">{{ variantName }}</h3>
        <p v-if="variantDescription">{{ variantDescription }}</p>
        <div class="text-overlay-400 mt-2 flex gap-4 text-sm">
          <span v-if="prepTime">Prep: {{ prepTime }} min</span>
          <span v-if="cookTime">Cook: {{ cookTime }} min</span>
          <span v-if="servings">Serves: {{ servings }}</span>
        </div>
      </div>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h4 class="mb-2 font-bold">Ingredients ({{ ingredientList.filter((i) => i.name.trim()).length }})</h4>
        <ul>
          <li v-for="(ing, i) in ingredientList.filter((i) => i.name.trim())" :key="i">
            <span v-if="!ing.isEyeballed && ing.quantity">{{ ing.quantity }} {{ ing.unit }}</span>
            <span v-else>to taste</span>
            — {{ ing.name }}
          </li>
        </ul>
      </div>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h4 class="mb-2 font-bold">Instructions ({{ instructionList.filter((i) => i.text.trim()).length }} steps)</h4>
        <ol class="list-inside list-decimal">
          <li v-for="(inst, i) in instructionList.filter((i) => i.text.trim())" :key="i">
            {{ inst.text }}
          </li>
        </ol>
      </div>

      <p v-if="submitError" class="text-red-500">{{ submitError }}</p>

      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          ← Back
        </button>
        <button
          type="button"
          :disabled="isSubmitting"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
          @click="handleSubmit"
        >
          {{ isSubmitting ? 'Submitting...' : 'Submit for Review' }}
        </button>
      </div>
    </div>
  </section>
</template>
