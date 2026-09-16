<!-- #region CreateRecipeView Component Properties -->
<script lang="ts">
  import { ref, computed } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import NumberStepper from '~/Components/Forms/NumberStepper.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { provideResourceStrings } from '~/Components/ResourceStrings'
  import { post } from '~/Utilities/Api'

  /**
   * Five-step wizard creating a recipe together with its first variant.
   */
  export default {
    name: 'CreateRecipeView',
  }

  export interface CreateRecipeViewProps {
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
  const props = defineProps<CreateRecipeViewProps>()

  provideResourceStrings(props.resourceStrings, 'CreateRecipe')

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
  const prepTime = ref<number | undefined>(0)
  const cookTime = ref<number | undefined>(0)
  const servings = ref<number | undefined>(0)

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

    const result = await post('/api/recipes', {
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
    })
    isSubmitting.value = false

    if (!result.success) {
      submitError.value = result.errorMessage
      return
    }

    submitSuccess.value = true
  }
</script>

<template>
  <SmallHero dark>
    <template #title><ResourceString for="CreateRecipe" /></template>
  </SmallHero>

  <section
    v-if="submitSuccess"
    v-ink="'sheet'"
    class="sk-sheet sk-sheet--lg sk-fold flex flex-col items-center gap-4 text-center"
  >
    <span class="sk-wash" style="--c: var(--color-green)" aria-hidden="true"></span>
    <h2 class="font-casual text-4xl">Recipe Submitted!</h2>
    <p class="sk-hand text-lg">Your recipe has been submitted for review. An admin will review and publish it.</p>
    <a href="/recipes" v-ink="'button'" class="sk-btn sk-btn--marker sk-btn--lg">Back to Recipes</a>
  </section>

  <section v-else>
    <!-- Progress bar -->
    <WizardProgress :current="step" :total="totalSteps" />

    <!-- Step 1: Recipe Basics -->
    <form v-if="step === 1" @submit.prevent="step++" v-ink="'sheet'" class="sk-sheet sk-tabbed relative flex flex-col gap-6">
      <h2 class="sk-tab"><i class="fa-duotone fa-pen-to-square" aria-hidden="true"></i>Recipe Basics</h2>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Recipe Name</span>
        <InputField v-model="recipeName" required type="text" placeholder="e.g., Mac & Cheese" />
      </label>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <TextAreaField v-model="recipeDescription" required placeholder="A short description of this dish" />
      </label>
      <Button type="submit" class="self-end" :disabled="!canProceed"> Next → </Button>
    </form>

    <!-- Step 2: Variant Info -->
    <form v-if="step === 2" @submit.prevent="step++" v-ink="'sheet'" class="sk-sheet sk-tabbed relative flex flex-col gap-6">
      <h2 class="sk-tab"><i class="fa-duotone fa-layer-group" aria-hidden="true"></i>First Variant</h2>
      <p class="text-ink-soft">Each recipe needs at least one variant — a specific way to make it.</p>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Variant Name</span>
        <InputField v-model="variantName" required type="text" placeholder="e.g., Classic Stovetop" />
      </label>
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <TextAreaField v-model="variantDescription" placeholder="What makes this variant special?" />
      </label>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-3">
        <div class="flex flex-col gap-2">
          <span class="text-lg font-bold">Prep Time</span>
          <NumberStepper v-model="prepTime" :min="0" unit="min" label="Prep Time" />
        </div>
        <div class="flex flex-col gap-2">
          <span class="text-lg font-bold">Cook Time</span>
          <NumberStepper v-model="cookTime" :min="0" unit="min" label="Cook Time" />
        </div>
        <div class="flex flex-col gap-2">
          <span class="text-lg font-bold">Servings</span>
          <NumberStepper v-model="servings" :min="0" label="Servings" />
        </div>
      </div>
      <div class="flex justify-between">
        <Button variant="ghost" @click="step--"> ← Back </Button>
        <Button type="submit" class="self-end" :disabled="!canProceed"> Next → </Button>
      </div>
    </form>

    <!-- Step 3: Ingredients -->
    <form v-if="step === 3" @submit.prevent="step++" v-ink="'sheet'" class="sk-sheet sk-tabbed relative flex flex-col gap-6">
      <h2 class="sk-tab"><i class="fa-duotone fa-list-check" aria-hidden="true"></i>Ingredients</h2>
      <div class="flex flex-col gap-4">
        <div class="flex gap-2" v-for="(ingredient, index) in ingredientList" :key="index">
          <InputField class="shrink grow basis-1/3" v-model="ingredient.name" placeholder="Ingredient name" type="text" />
          <button
            type="button"
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-paper p-2 text-ink"
            @click="ingredient.isEyeballed = !ingredient.isEyeballed"
          >
            <i :class="ingredient.isEyeballed ? 'fa-duotone fa-eye' : 'fa-duotone fa-eye-slash'"></i>
          </button>
          <InputField
            class="shrink basis-1/6"
            v-model="ingredient.quantity"
            placeholder="Qty"
            type="number"
            inputmode="decimal"
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
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-marker p-2 text-marker-ink disabled:cursor-not-allowed disabled:opacity-45"
            @click="ingredientList.splice(index, 1)"
          >
            <i class="fa-duotone fa-trash"></i>
          </button>
        </div>
      </div>
      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-paper px-4 py-2 text-ink"
        @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })"
      >
        Add Ingredient
      </button>
      <div class="flex justify-between">
        <Button variant="ghost" @click="step--"> ← Back </Button>
        <Button type="submit" class="self-end" :disabled="!canProceed"> Next → </Button>
      </div>
    </form>

    <!-- Step 4: Instructions -->
    <form v-if="step === 4" @submit.prevent="step++" v-ink="'sheet'" class="sk-sheet sk-tabbed relative flex flex-col gap-6">
      <h2 class="sk-tab"><i class="fa-duotone fa-list-ol" aria-hidden="true"></i>Instructions</h2>
      <div class="flex flex-col gap-4">
        <div class="flex gap-4" v-for="(instruction, index) in instructionList" :key="index">
          <p class="max-w-12 pt-1.5 text-end text-2xl">{{ index + 1 }}.</p>
          <TextAreaField class="shrink grow" v-model="instruction.text" placeholder="Describe this step" />
          <button
            type="button"
            :disabled="instructionList.length <= 1"
            class="h-12 max-w-12 cursor-pointer self-center rounded-2xl bg-marker p-2 text-marker-ink disabled:cursor-not-allowed disabled:opacity-45"
            @click="instructionList.splice(index, 1)"
          >
            <i class="fa-duotone fa-trash"></i>
          </button>
        </div>
      </div>
      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-paper px-4 py-2 text-ink"
        @click="instructionList.push({ text: '' })"
      >
        Add Step
      </button>
      <div class="flex justify-between">
        <Button variant="ghost" @click="step--"> ← Back </Button>
        <Button type="submit" class="self-end" :disabled="!canProceed"> Next → </Button>
      </div>
    </form>

    <!-- Step 5: Review & Submit -->
    <div v-if="step === 5" v-ink="'sheet'" class="sk-sheet sk-tabbed relative flex flex-col gap-6">
      <h2 class="sk-tab"><i class="fa-duotone fa-clipboard-check" aria-hidden="true"></i>Review & Submit</h2>

      <div class="rounded-3xl bg-paper-2 p-6 text-ink">
        <h3 class="font-casual text-3xl">{{ recipeName }}</h3>
        <p>{{ recipeDescription }}</p>
      </div>

      <div class="rounded-3xl bg-paper-2 p-6 text-ink">
        <h3 class="text-xl font-bold">{{ variantName }}</h3>
        <p v-if="variantDescription">{{ variantDescription }}</p>
        <div class="mt-2 flex gap-4 text-sm text-ink-soft">
          <span v-if="prepTime">Prep: {{ prepTime }} min</span>
          <span v-if="cookTime">Cook: {{ cookTime }} min</span>
          <span v-if="servings">Serves: {{ servings }}</span>
        </div>
      </div>

      <div class="rounded-3xl bg-paper-2 p-6 text-ink">
        <h4 class="mb-2 font-bold">Ingredients ({{ ingredientList.filter((i) => i.name.trim()).length }})</h4>
        <ul>
          <li v-for="(ing, i) in ingredientList.filter((i) => i.name.trim())" :key="i">
            <span v-if="!ing.isEyeballed && ing.quantity">{{ ing.quantity }} {{ ing.unit }}</span>
            <span v-else>to taste</span>
            — {{ ing.name }}
          </li>
        </ul>
      </div>

      <div class="rounded-3xl bg-paper-2 p-6 text-ink">
        <h4 class="mb-2 font-bold">Instructions ({{ instructionList.filter((i) => i.text.trim()).length }} steps)</h4>
        <ol class="list-inside list-decimal">
          <li v-for="(inst, i) in instructionList.filter((i) => i.text.trim())" :key="i">
            {{ inst.text }}
          </li>
        </ol>
      </div>

      <p v-if="submitError" class="text-danger-ink">{{ submitError }}</p>

      <div class="flex justify-between">
        <Button variant="ghost" @click="step--"> ← Back </Button>
        <Button :disabled="isSubmitting" @click="handleSubmit">
          {{ isSubmitting ? 'Submitting...' : 'Submit for Review' }}
        </Button>
      </div>
    </div>
  </section>
</template>
