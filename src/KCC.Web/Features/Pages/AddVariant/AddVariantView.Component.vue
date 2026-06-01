<script setup lang="ts">
  import { ref, computed } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { cx } from '~/Utilities/CX'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  const props = defineProps<{
    recipeId: string
    recipeName: string
    recipeSlug: string
    antiforgeryToken?: string
    resourceStrings?: Record<string, string>
  }>()

  const rs = useResourceStrings(props.resourceStrings, 'AddVariant')

  const step = ref(1)
  const totalSteps = 4
  const isSubmitting = ref(false)
  const submitError = ref('')
  const submitSuccess = ref(false)

  const variantName = ref('')
  const variantDescription = ref('')
  const prepTime = ref<number | undefined>()
  const cookTime = ref<number | undefined>()
  const servings = ref<number | undefined>()
  const ingredientList = ref<Ingredient[]>([{ name: '', unit: '', isEyeballed: false }])
  const instructionList = ref<Instruction[]>([{ text: '' }])

  /* eslint-disable vue/script-indent */
  const canProceed = computed(() => {
    switch (step.value) {
      case 1:
        return variantName.value.trim() !== ''
      case 2:
        return ingredientList.value.some((i) => i.name.trim() !== '')
      case 3:
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
      const response = await fetch(`/api/recipes/${props.recipeId}/variants`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(props.antiforgeryToken ? { RequestVerificationToken: props.antiforgeryToken } : {}),
        },
        body: JSON.stringify({
          variantName: variantName.value.trim(),
          variantDescription: variantDescription.value.trim(),
          prepTime: prepTime.value || null,
          cookTime: cookTime.value || null,
          servings: servings.value || null,
          ingredients: ingredientList.value.filter((i) => i.name.trim() !== ''),
          instructions: instructionList.value
            .filter((i) => i.text.trim() !== '')
            .map((i, idx) => ({ step: idx + 1, text: i.text.trim() })),
        }),
      })

      if (!response.ok) {
        const data = await response.json()
        submitError.value = data.error || rs('FailedToAddVariant')
        return
      }

      submitSuccess.value = true
    } catch {
      submitError.value = rs('UnexpectedError')
    } finally {
      isSubmitting.value = false
    }
  }
</script>

<template>
  <SmallHero dark>
    <template #title>
      <ResourceString for="AddVariantFor" class="mr-2" /><span>{{ recipeName }}</span>
    </template>

    <template #action-button>
      <AppLink :href="recipeSlug" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx">
        <ResourceString for="Cancel" />
        <i class="fa-solid fa-x"></i>
      </AppLink>
    </template>
  </SmallHero>

  <section v-if="submitSuccess" class="flex flex-col items-center gap-4 py-12 text-center">
    <ResourceString for="VariantSubmitted" as="h2" class="font-casual text-4xl" />
    <ResourceString for="VariantSubmittedMessage" as="p" class="text-lg" />

    <AppLink :href="recipeSlug" class="rounded-3xl bg-surface-500 px-6 py-3 text-xl text-bone">
      <ResourceString for="BackTo" class="mr-1" />
      <span>{{ recipeName }}</span>
    </AppLink>
  </section>

  <section v-else>
    <div class="mb-8 flex gap-2">
      <div
        v-for="stepIndex in totalSteps"
        :key="stepIndex"
        :class="
          cx(
            'h-2 flex-1 rounded-full transition-colors duration-300',
            stepIndex <= step ? 'bg-surface-500' : 'bg-overlay-300',
          )
        "
      />
    </div>

    <!-- Step 1: Variant Info -->
    <form v-if="step === 1" @submit.prevent="step++" class="flex flex-col items-start gap-6">
      <ResourceString for="VariantInfo" as="h2" class="text-2xl font-bold" />

      <label class="flex w-full flex-col items-start gap-2">
        <ResourceString for="VariantName" class="text-lg font-bold" />
        <InputField v-model="variantName" required type="text" />
      </label>

      <label class="flex w-full flex-col items-start gap-2">
        <ResourceString for="Description" class="text-lg font-bold" />
        <TextAreaField v-model="variantDescription" :placeholder="rs('DescriptionPlaceholder')" />
      </label>

      <div class="grid w-full grid-cols-1 gap-4 md:grid-cols-3">
        <label class="flex flex-col items-start gap-2">
          <ResourceString for="PrepTime" class="text-lg font-bold" />
          <InputField v-model="prepTime" type="number" />
        </label>

        <label class="flex flex-col items-start gap-2">
          <ResourceString for="CookTime" class="text-lg font-bold" />
          <InputField v-model="cookTime" type="number" />
        </label>

        <label class="flex flex-col items-start gap-2">
          <ResourceString for="Servings" class="text-lg font-bold" />
          <InputField v-model="servings" type="number" />
        </label>
      </div>

      <button
        type="submit"
        :disabled="!canProceed"
        class="cursor-pointer self-end rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
      >
        <ResourceString for="Next" />
        <i class="fa-solid fa-arrow-right"></i>
      </button>
    </form>

    <!-- Step 2: Ingredients -->
    <form v-if="step === 2" @submit.prevent="step++" class="flex flex-col gap-6">
      <ResourceString for="Ingredients" as="h2" class="text-2xl font-bold" />

      <div class="flex flex-col gap-4">
        <div v-for="(ingredient, index) in ingredientList" :key="index" class="flex gap-2">
          <label class="shrink grow basis-1/3">
            <ResourceString for="IngredientName" class="sr-only" />
            <InputField v-model="ingredient.name" :placeholder="rs('IngredientNamePlaceholder')" type="text" />
          </label>

          <button
            type="button"
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white"
            @click="ingredient.isEyeballed = !ingredient.isEyeballed"
          >
            <ResourceString for="Eyeball" class="sr-only" />
            <i :class="ingredient.isEyeballed ? 'fa-solid fa-eye' : 'fa-solid fa-eye-slash'"></i>
          </button>

          <label class="shrink basis-1/6">
            <ResourceString for="Quantity" class="sr-only" />
            <InputField
              v-model="ingredient.quantity"
              :placeholder="rs('QuantityPlaceholder')"
              type="number"
              step="0.01"
              :disabled="ingredient.isEyeballed"
            />
          </label>

          <label class="shrink basis-1/6">
            <ResourceString for="Unit" class="sr-only" />
            <InputField
              v-model="ingredient.unit"
              :placeholder="rs('UnitPlaceholder')"
              type="text"
              :disabled="ingredient.isEyeballed"
            />
          </label>

          <button
            type="button"
            :disabled="ingredientList.length <= 1"
            class="h-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="ingredientList.splice(index, 1)"
          >
            <ResourceString for="Remove" class="sr-only" />
            <i class="fa-solid fa-trash"></i>
          </button>
        </div>
      </div>

      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-surface-500 px-4 py-2 text-bone"
        @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })"
      >
        <ResourceString for="AddIngredient" />
      </button>

      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <FontAwesomeIcon :icon="faArrowLeft" />
          <ResourceString for="Back" class="ml-2" />
        </button>

        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          <ResourceString for="Next" class="mr-2" />
          <FontAwesomeIcon :icon="faArrowRight" />
        </button>
      </div>
    </form>

    <!-- Step 3: Instructions -->
    <form v-if="step === 3" @submit.prevent="step++" class="flex flex-col gap-6">
      <ResourceString for="Instructions" as="h2" class="text-2xl font-bold" />

      <div class="flex flex-col gap-4">
        <div class="flex gap-4" v-for="(instruction, index) in instructionList" :key="index">
          <p class="max-w-12 pt-1.5 text-end text-2xl">{{ index + 1 }}.</p>

          <TextAreaField class="shrink grow" v-model="instruction.text" :placeholder="rs('DescribeThisStep')" />

          <button
            type="button"
            :disabled="instructionList.length <= 1"
            class="h-12 max-w-12 cursor-pointer self-center rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="instructionList.splice(index, 1)"
          >
            <ResourceString for="Remove" class="sr-only" />
            <i class="fa-solid fa-trash"></i>
          </button>
        </div>
      </div>

      <button
        type="button"
        class="cursor-pointer self-start rounded-3xl bg-surface-500 px-4 py-2 text-bone"
        @click="instructionList.push({ text: '' })"
      >
        <ResourceString for="AddStep" />
      </button>

      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <ResourceString for="Back" class="ml-2" />
          <FontAwesomeIcon :icon="faArrowLeft" />
        </button>

        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          <ResourceString for="Next" class="mr-2" />
          <FontAwesomeIcon :icon="faArrowRight" />
        </button>
      </div>
    </form>

    <!-- Step 4: Review & Submit -->
    <div v-if="step === 4" class="flex flex-col gap-6">
      <ResourceString for="ReviewAndSubmit" as="h2" class="text-2xl font-bold" />

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h3 class="text-xl font-bold">{{ variantName }}</h3>
        <p v-if="variantDescription">{{ variantDescription }}</p>

        <div class="mt-2 flex gap-4 text-sm text-overlay-400">
          <span v-if="prepTime"><ResourceString for="PrepTime" />: {{ prepTime }} <ResourceString for="Min" /></span>
          <span v-if="cookTime"><ResourceString for="CookTime" />: {{ cookTime }} <ResourceString for="Min" /></span>
          <span v-if="servings"><ResourceString for="Serves" />: {{ servings }}</span>
        </div>
      </div>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h4 class="mb-2 font-bold">
          <ResourceString for="Ingredients" class="mr-2" /> ({{ ingredientList.filter((i) => i.name.trim()).length }})
        </h4>

        <ul>
          <li v-for="(ing, i) in ingredientList.filter((i) => i.name.trim())" :key="i">
            <span v-if="!ing.isEyeballed && ing.quantity">{{ ing.quantity }} {{ ing.unit }}</span>
            <span v-else><ResourceString for="ToTaste" /></span> — {{ ing.name }}
          </li>
        </ul>
      </div>

      <div class="rounded-3xl bg-bone p-6 text-onyx">
        <h4 class="mb-2 font-bold">
          <ResourceString for="Instructions" class="mr-2" />
          <span>({{ instructionList.filter((i) => i.text.trim()).length }}</span>
          <ResourceString for="Steps" />)
        </h4>

        <ol class="list-inside list-decimal">
          <li v-for="(inst, i) in instructionList.filter((i) => i.text.trim())" :key="i">{{ inst.text }}</li>
        </ol>
      </div>

      <p v-if="submitError" class="text-red-500">{{ submitError }}</p>

      <div class="flex justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <FontAwesomeIcon :icon="faArrowLeft" />
          <ResourceString for="Back" class="ml-2" />
        </button>

        <button
          type="button"
          :disabled="isSubmitting"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
          @click="handleSubmit"
        >
          {{ isSubmitting ? rs('Submitting') : rs('SubmitForReview') }}
        </button>
      </div>
    </div>
  </section>
</template>
