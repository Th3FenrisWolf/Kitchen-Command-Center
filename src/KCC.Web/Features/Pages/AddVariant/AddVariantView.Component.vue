<script setup lang="ts">
  import { ref, computed } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import NumberStepper from '~/Components/Forms/NumberStepper.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import { post } from '~/Utilities/Api'

  const props = defineProps<{
    recipeId: string
    recipeName: string
    recipeSlug: string
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
  const prepTime = ref<number | undefined>(0)
  const cookTime = ref<number | undefined>(0)
  const servings = ref<number | undefined>(0)
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

    const result = await post(`/api/recipes/${props.recipeId}/variants`, {
      variantName: variantName.value.trim(),
      variantDescription: variantDescription.value.trim(),
      prepTime: prepTime.value || null,
      cookTime: cookTime.value || null,
      servings: servings.value || null,
      ingredients: ingredientList.value.filter((i) => i.name.trim() !== ''),
      instructions: instructionList.value
        .filter((i) => i.text.trim() !== '')
        .map((i, idx) => ({ step: idx + 1, text: i.text.trim() })),
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
    <template #title>
      <ResourceString for="AddVariantFor" class="mr-2" /><span>{{ recipeName }}</span>
    </template>

    <template #action-button>
      <AppLink :href="recipeSlug" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx">
        <ResourceString for="Cancel" />
        <i class="fa-solid fa-close fa-sm"></i>
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
        :class="[
          'h-2 flex-1 rounded-full transition-colors duration-300',
          stepIndex <= step ? 'bg-surface-500' : 'bg-overlay-300',
        ]"
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
        <div class="space-y-2">
          <ResourceString for="PrepTime" as="p" class="text-lg font-bold" />
          <NumberStepper v-model="prepTime" :min="0" :unit="rs('Min')" :label="rs('PrepTime')" />
        </div>

        <div class="space-y-2">
          <ResourceString for="CookTime" as="p" class="text-lg font-bold" />
          <NumberStepper v-model="cookTime" :min="0" :unit="rs('Min')" :label="rs('CookTime')" />
        </div>

        <div class="space-y-2">
          <ResourceString for="Servings" as="p" class="text-lg font-bold" />
          <NumberStepper v-model="servings" :min="0" :label="rs('Servings')" />
        </div>
      </div>

      <button
        type="submit"
        :disabled="!canProceed"
        class="cursor-pointer self-end rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
      >
        <ResourceString for="Next" />
        <i class="fa-solid fa-arrow-right fa-sm"></i>
      </button>
    </form>

    <!-- Step 2: Ingredients -->
    <form v-if="step === 2" @submit.prevent="step++" class="flex flex-col items-start gap-6">
      <ResourceString for="Ingredients" as="h2" class="text-2xl font-bold" />

      <div class="flex w-full flex-col gap-4">
        <div v-for="(ingredient, index) in ingredientList" :key="index" class="flex gap-2">
          <label class="shrink grow basis-1/3">
            <ResourceString for="IngredientName" class="sr-only" />
            <InputField v-model="ingredient.name" :placeholder="rs('IngredientNamePlaceholder')" type="text" />
          </label>

          <button
            type="button"
            class="size-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white"
            @click="ingredient.isEyeballed = !ingredient.isEyeballed"
          >
            <ResourceString for="Eyeball" class="sr-only" />
            <i :class="ingredient.isEyeballed ? 'fa-duotone fa-eye' : 'fa-duotone fa-eye-slash'"></i>
          </button>

          <label class="shrink basis-1/6">
            <ResourceString for="Quantity" class="sr-only" />
            <InputField
              v-model="ingredient.quantity"
              :placeholder="rs('QuantityPlaceholder')"
              type="number"
              inputmode="decimal"
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
            class="size-12 max-w-12 basis-1/12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="ingredientList.splice(index, 1)"
          >
            <ResourceString for="Remove" class="sr-only" />
            <i class="fa-duotone fa-trash"></i>
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

      <div class="flex w-full justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <i class="fa-solid fa-arrow-left fa-sm"></i>
          <ResourceString for="Back" class="ml-2" />
        </button>

        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          <ResourceString for="Next" class="mr-2" />
          <i class="fa-solid fa-arrow-right fa-sm"></i>
        </button>
      </div>
    </form>

    <!-- Step 3: Instructions -->
    <form v-if="step === 3" @submit.prevent="step++" class="flex flex-col items-start gap-6">
      <ResourceString for="Instructions" as="h2" class="text-2xl font-bold" />

      <div class="flex w-full flex-col gap-4">
        <div class="flex items-center gap-4" v-for="(instruction, index) in instructionList" :key="index">
          <h2>{{ index + 1 }}.</h2>

          <TextAreaField class="shrink grow" v-model="instruction.text" :placeholder="rs('DescribeThisStep')" />

          <button
            type="button"
            :disabled="instructionList.length <= 1"
            class="size-12 max-w-12 cursor-pointer rounded-2xl bg-surface-500 p-2 text-white disabled:cursor-not-allowed disabled:bg-overlay-300"
            @click="instructionList.splice(index, 1)"
          >
            <ResourceString for="Remove" class="sr-only" />
            <i class="fa-duotone fa-trash fa-sm"></i>
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

      <div class="flex w-full justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <i class="fa-solid fa-arrow-left fa-sm"></i>
          <ResourceString for="Back" class="ml-2" />
        </button>

        <button
          type="submit"
          :disabled="!canProceed"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
        >
          <ResourceString for="Next" class="mr-2" />
          <i class="fa-solid fa-arrow-right fa-sm"></i>
        </button>
      </div>
    </form>

    <!-- Step 4: Review & Submit -->
    <div v-if="step === 4" class="flex flex-col items-start gap-6">
      <ResourceString for="ReviewAndSubmit" as="h2" class="text-2xl font-bold" />

      <div class="rounded-3xl bg-bone text-onyx">
        <h3 class="text-xl font-bold">{{ variantName }}</h3>
        <p v-if="variantDescription">{{ variantDescription }}</p>

        <div class="mt-2 flex gap-4 text-sm text-overlay-400">
          <span v-if="prepTime"><ResourceString for="PrepTime" />: {{ prepTime }} <ResourceString for="Min" /></span>
          <span v-if="cookTime"><ResourceString for="CookTime" />: {{ cookTime }} <ResourceString for="Min" /></span>
          <span v-if="servings"><ResourceString for="Serves" />: {{ servings }}</span>
        </div>
      </div>

      <div class="rounded-3xl bg-bone text-onyx">
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

      <div class="rounded-3xl bg-bone text-onyx">
        <h4 class="mb-2 font-bold">
          <ResourceString for="Instructions" class="mr-2" />
          <span class="mr-2">({{ instructionList.filter((i) => i.text.trim()).length }}</span>
          <ResourceString :for="instructionList.length === 1 ? 'Step' : 'Steps'" />)
        </h4>

        <ol class="list-inside list-decimal">
          <li v-for="(inst, i) in instructionList.filter((i) => i.text.trim())" :key="i">{{ inst.text }}</li>
        </ol>
      </div>

      <p v-if="submitError" class="text-red-500">{{ submitError }}</p>

      <div class="flex w-full justify-between">
        <button type="button" class="cursor-pointer rounded-3xl border border-surface-500 px-6 py-2 text-xl" @click="step--">
          <i class="fa-solid fa-arrow-left fa-sm"></i>
          <ResourceString for="Back" class="ml-2" />
        </button>

        <button
          type="button"
          :disabled="isSubmitting"
          class="cursor-pointer rounded-3xl bg-surface-500 px-6 py-2 text-xl text-bone disabled:cursor-not-allowed disabled:opacity-50"
          @click="handleSubmit"
        >
          <ResourceString :for="isSubmitting ? 'Submitting' : 'SubmitForReview'" />
        </button>
      </div>
    </div>
  </section>
</template>
