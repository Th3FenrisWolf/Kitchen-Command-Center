<!-- #region CreateRecipeView Component Properties -->
<script lang="ts">
  import { ref, computed, useId } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import Field from '~/Components/Forms/Field.vue'
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
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
  const props = defineProps<CreateRecipeViewProps>()

  provideResourceStrings(props.resourceStrings, 'CreateRecipe')

  // 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
  const STEP_PAD = 'clamp(24px, 7.5vw, 48px)'

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

  const uid = useId()
  const ids = {
    recipeName: `${uid}-recipe-name`,
    recipeDescription: `${uid}-recipe-description`,
    variantName: `${uid}-variant-name`,
    variantDescription: `${uid}-variant-description`,
    prepTime: `${uid}-prep-time`,
    cookTime: `${uid}-cook-time`,
    servings: `${uid}-servings`,
  }

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

  const reviewIngredients = computed(() => ingredientList.value.filter((i) => i.name.trim() !== ''))
  const reviewInstructions = computed(() => instructionList.value.filter((i) => i.text.trim() !== ''))

  const stepNumber = (index: number) => String(index + 1).padStart(2, '0')

  const reviewAmount = (ingredient: Ingredient) =>
    !ingredient.isEyeballed && ingredient.quantity ? `${ingredient.quantity} ${ingredient.unit}`.trim() : 'to taste'

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

  <KccSheet v-if="submitSuccess" class="mt-6" wash="green" :at="{ x: '88%', y: '18%', w: '38%', h: '60%' }" :tear="2" tape>
    <div class="grid justify-items-center gap-6 text-center">
      <h2 class="kcc-h3">Recipe Submitted!</h2>
      <p class="kcc-hand">Your recipe has been submitted for review. An admin will review and publish it.</p>
      <Button as="a" href="/recipes" variant="marker" size="lg">Back to Recipes</Button>
    </div>
  </KccSheet>

  <section v-else class="mt-6">
    <WizardProgress :current="step" :total="totalSteps" />

    <KccSheet v-if="step === 1" crisp :tear="1" :pad="STEP_PAD" icon="fa-duotone fa-pen-to-square" label="Recipe Basics">
      <!-- The sheet's label carries the step's name in print; the heading carries it in the document. -->
      <h2 class="sr-only">Recipe Basics</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <Field label="Recipe Name" :control-id="ids.recipeName" required>
          <InputField :id="ids.recipeName" v-model="recipeName" required type="text" placeholder="e.g., Mac & Cheese" />
        </Field>

        <Field label="Description" :control-id="ids.recipeDescription" required>
          <TextAreaField
            :id="ids.recipeDescription"
            v-model="recipeDescription"
            required
            placeholder="A short description of this dish"
          />
        </Field>

        <div class="flex justify-end">
          <Button type="submit" :disabled="!canProceed"
            >Next<i class="fa-duotone fa-arrow-right" aria-hidden="true"></i
          ></Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 2" crisp :tear="2" :pad="STEP_PAD" icon="fa-duotone fa-layer-group" label="First Variant">
      <h2 class="sr-only">First Variant</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <p class="kcc-body text-ink-soft">Each recipe needs at least one variant — a specific way to make it.</p>

        <Field label="Variant Name" :control-id="ids.variantName" required>
          <InputField
            :id="ids.variantName"
            v-model="variantName"
            required
            type="text"
            placeholder="e.g., Classic Stovetop"
          />
        </Field>

        <Field label="Description" :control-id="ids.variantDescription">
          <TextAreaField
            :id="ids.variantDescription"
            v-model="variantDescription"
            placeholder="What makes this variant special?"
          />
        </Field>

        <div class="grid gap-x-7 gap-y-6 md:grid-cols-3">
          <Field label="Prep Time" :control-id="ids.prepTime">
            <NumberStepper :id="ids.prepTime" v-model="prepTime" :min="0" unit="min" label="Prep Time" />
          </Field>

          <Field label="Cook Time" :control-id="ids.cookTime">
            <NumberStepper :id="ids.cookTime" v-model="cookTime" :min="0" unit="min" label="Cook Time" />
          </Field>

          <Field label="Servings" :control-id="ids.servings">
            <NumberStepper :id="ids.servings" v-model="servings" :min="0" label="Servings" />
          </Field>
        </div>

        <div class="flex flex-wrap justify-between gap-3">
          <Button variant="ghost" @click="step--"><i class="fa-duotone fa-arrow-left" aria-hidden="true"></i>Back</Button>
          <Button type="submit" :disabled="!canProceed"
            >Next<i class="fa-duotone fa-arrow-right" aria-hidden="true"></i
          ></Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 3" crisp :tear="3" :pad="STEP_PAD" icon="fa-duotone fa-list-check" label="Ingredients">
      <h2 class="sr-only">Ingredients</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <ul class="flex flex-col gap-3">
          <li v-for="(ingredient, index) in ingredientList" :key="index" class="flex flex-wrap items-center gap-2">
            <InputField
              v-model="ingredient.name"
              class="min-w-40 shrink grow basis-1/3"
              type="text"
              aria-label="Ingredient name"
              placeholder="Ingredient name"
            />

            <Button
              :variant="ingredient.isEyeballed ? 'ink' : 'ghost'"
              class="kcc-btn--icon shrink-0"
              aria-label="Eyeball this ingredient"
              :aria-pressed="ingredient.isEyeballed"
              @click="ingredient.isEyeballed = !ingredient.isEyeballed"
            >
              <i :class="ingredient.isEyeballed ? 'fa-duotone fa-eye' : 'fa-duotone fa-eye-slash'" aria-hidden="true"></i>
            </Button>

            <InputField
              v-model="ingredient.quantity"
              class="shrink basis-20"
              type="number"
              inputmode="decimal"
              step="0.01"
              aria-label="Quantity"
              placeholder="Qty"
              :disabled="ingredient.isEyeballed"
            />

            <InputField
              v-model="ingredient.unit"
              class="shrink basis-20"
              type="text"
              aria-label="Unit"
              placeholder="Unit"
              :disabled="ingredient.isEyeballed"
            />

            <Button
              variant="ghost"
              class="kcc-btn--icon shrink-0"
              aria-label="Remove Ingredient"
              :disabled="ingredientList.length <= 1"
              @click="ingredientList.splice(index, 1)"
            >
              <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
            </Button>
          </li>
        </ul>

        <Button variant="ghost" class="self-start" @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })">
          <i class="fa-duotone fa-plus" aria-hidden="true"></i>Add Ingredient
        </Button>

        <div class="flex flex-wrap justify-between gap-3">
          <Button variant="ghost" @click="step--"><i class="fa-duotone fa-arrow-left" aria-hidden="true"></i>Back</Button>
          <Button type="submit" :disabled="!canProceed"
            >Next<i class="fa-duotone fa-arrow-right" aria-hidden="true"></i
          ></Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 4" crisp :tear="4" :pad="STEP_PAD" icon="fa-duotone fa-list-ol" label="Instructions">
      <h2 class="sr-only">Instructions</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <ol class="kcc-steps">
          <li v-for="(instruction, index) in instructionList" :key="index" class="grid-cols-[28px_minmax(0,1fr)_auto]">
            <span class="kcc-n">{{ stepNumber(index) }}</span>

            <TextAreaField v-model="instruction.text" aria-label="Describe this step" placeholder="Describe this step" />

            <Button
              variant="ghost"
              class="kcc-btn--icon"
              aria-label="Remove Step"
              :disabled="instructionList.length <= 1"
              @click="instructionList.splice(index, 1)"
            >
              <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
            </Button>
          </li>
        </ol>

        <Button variant="ghost" class="self-start" @click="instructionList.push({ text: '' })">
          <i class="fa-duotone fa-plus" aria-hidden="true"></i>Add Step
        </Button>

        <div class="flex flex-wrap justify-between gap-3">
          <Button variant="ghost" @click="step--"><i class="fa-duotone fa-arrow-left" aria-hidden="true"></i>Back</Button>
          <Button type="submit" :disabled="!canProceed"
            >Next<i class="fa-duotone fa-arrow-right" aria-hidden="true"></i
          ></Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 5" crisp :tear="5" :pad="STEP_PAD" icon="fa-duotone fa-clipboard-check" label="Review & Submit">
      <h2 class="sr-only">Review & Submit</h2>

      <div class="space-y-12">
        <div>
          <p class="kcc-kick">Recipe</p>
          <h3 class="kcc-h4 mt-6">{{ recipeName }}</h3>
          <p class="kcc-body mt-6">{{ recipeDescription }}</p>
        </div>

        <div>
          <p class="kcc-kick">First variant</p>
          <h3 class="kcc-h4 mt-6">{{ variantName }}</h3>
          <p v-if="variantDescription" class="kcc-body mt-6">{{ variantDescription }}</p>

          <div v-if="prepTime || cookTime || servings" class="kcc-stats mt-6">
            <div v-if="prepTime">
              <p class="kcc-lbl">Prep</p>
              <p class="kcc-v">{{ prepTime }}<small>min</small></p>
            </div>

            <div v-if="cookTime">
              <p class="kcc-lbl">Cook</p>
              <p class="kcc-v">{{ cookTime }}<small>min</small></p>
            </div>

            <div v-if="servings">
              <p class="kcc-lbl">Serves</p>
              <p class="kcc-v">{{ servings }}</p>
            </div>
          </div>
        </div>

        <div>
          <h4 class="kcc-kick flex items-baseline gap-2">
            Ingredients<span class="kcc-num">{{ reviewIngredients.length }}</span>
          </h4>

          <ul class="kcc-check mt-6">
            <li v-for="(ing, i) in reviewIngredients" :key="i" class="grid-cols-[1fr_auto]">
              <span>{{ ing.name }}</span>
              <span class="kcc-q">{{ reviewAmount(ing) }}</span>
            </li>
          </ul>
        </div>

        <div>
          <h4 class="kcc-kick flex items-baseline gap-2">
            Instructions<span class="kcc-num">{{ reviewInstructions.length }}</span
            >steps
          </h4>

          <ol class="kcc-steps mt-6">
            <li v-for="(inst, i) in reviewInstructions" :key="i">
              <span class="kcc-n">{{ stepNumber(i) }}</span>
              <p class="kcc-body">{{ inst.text }}</p>
            </li>
          </ol>
        </div>
      </div>

      <p v-if="submitError" class="kcc-well kcc-well--danger kcc-kick mt-6" role="alert">{{ submitError }}</p>

      <div class="mt-12 flex flex-wrap justify-between gap-3">
        <Button variant="ghost" @click="step--"><i class="fa-duotone fa-arrow-left" aria-hidden="true"></i>Back</Button>
        <Button :disabled="isSubmitting" @click="handleSubmit">{{
          isSubmitting ? 'Submitting...' : 'Submit for Review'
        }}</Button>
      </div>
    </KccSheet>
  </section>
</template>
