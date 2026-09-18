<!-- #region AddVariantView Component Properties -->
<script lang="ts">
  import { ref, computed, useId } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import NumberStepper from '~/Components/Forms/NumberStepper.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { post } from '~/Utilities/Api'
  import { stepLabelKey, validIngredients, validInstructions } from '~/Pages/AddVariant/reviewSummary'

  /**
   * Four-step wizard adding a variant to an existing recipe.
   */
  export default {
    name: 'AddVariantView',
  }

  export interface AddVariantViewProps {
    /**
     * GUID of the parent recipe, submitted with the finished variant.
     */
    recipeId: string
    recipeName: string
    /**
     * Where to return once the variant is submitted.
     */
    recipeSlug: string
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
  const props = defineProps<AddVariantViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'AddVariant')

  // 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
  const STEP_PAD = 'clamp(24px, 7.5vw, 48px)'

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

  const uid = useId()
  const ids = {
    variantName: `${uid}-variant-name`,
    variantDescription: `${uid}-variant-description`,
    prepTime: `${uid}-prep-time`,
    cookTime: `${uid}-cook-time`,
    servings: `${uid}-servings`,
  }

  // Kentico hands out tilde-relative paths; the tilde is stripped before it reaches an anchor.
  const recipeHref = computed(() => props.recipeSlug.stripTilde())

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

  const reviewIngredients = computed(() => validIngredients(ingredientList.value))
  const reviewInstructions = computed(() => validInstructions(instructionList.value))

  const stepNumber = (index: number) => String(index + 1).padStart(2, '0')

  const reviewAmount = (ingredient: Ingredient) =>
    !ingredient.isEyeballed && ingredient.quantity ? `${ingredient.quantity} ${ingredient.unit}`.trim() : rs('ToTaste')

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
      <Button as="a" :href="recipeHref" variant="ghost">
        <ResourceString for="Cancel" /><i class="fa-duotone fa-xmark" aria-hidden="true"></i>
      </Button>
    </template>
  </SmallHero>

  <KccSheet v-if="submitSuccess" class="mt-6" wash="green" :at="{ x: '88%', y: '18%', w: '38%', h: '60%' }" :tear="2" tape>
    <div class="grid justify-items-center gap-6 text-center">
      <ResourceString for="VariantSubmitted" as="h2" class="kcc-h3" />
      <ResourceString for="VariantSubmittedMessage" as="p" class="kcc-hand" />

      <Button as="a" :href="recipeHref" variant="marker" size="lg">
        <ResourceString for="BackTo" /><span>{{ recipeName }}</span>
      </Button>
    </div>
  </KccSheet>

  <section v-else class="mt-6">
    <WizardProgress :current="step" :total="totalSteps" />

    <KccSheet v-if="step === 1" crisp :tear="1" :pad="STEP_PAD" icon="fa-duotone fa-pen-to-square">
      <template #label><ResourceString for="VariantInfo" /></template>

      <!-- The sheet's label carries the step's name in print; the heading carries it in the document. -->
      <h2 class="sr-only">{{ rs('VariantInfo') }}</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <Field :label="rs('VariantName')" :control-id="ids.variantName" required>
          <InputField :id="ids.variantName" v-model="variantName" required type="text" />
        </Field>

        <Field :label="rs('Description')" :control-id="ids.variantDescription">
          <TextAreaField
            :id="ids.variantDescription"
            v-model="variantDescription"
            :placeholder="rs('DescriptionPlaceholder')"
          />
        </Field>

        <div class="grid gap-x-7 gap-y-6 md:grid-cols-3">
          <Field :label="rs('PrepTime')" :control-id="ids.prepTime">
            <NumberStepper :id="ids.prepTime" v-model="prepTime" :min="0" :unit="rs('Min')" :label="rs('PrepTime')" />
          </Field>

          <Field :label="rs('CookTime')" :control-id="ids.cookTime">
            <NumberStepper :id="ids.cookTime" v-model="cookTime" :min="0" :unit="rs('Min')" :label="rs('CookTime')" />
          </Field>

          <Field :label="rs('Servings')" :control-id="ids.servings">
            <NumberStepper :id="ids.servings" v-model="servings" :min="0" :label="rs('Servings')" />
          </Field>
        </div>

        <div class="flex justify-end">
          <Button type="submit" :disabled="!canProceed">
            <ResourceString for="Next" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
          </Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 2" crisp :tear="2" :pad="STEP_PAD" icon="fa-duotone fa-list-check">
      <template #label><ResourceString for="Ingredients" /></template>

      <h2 class="sr-only">{{ rs('Ingredients') }}</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <ul class="flex flex-col gap-3">
          <li v-for="(ingredient, index) in ingredientList" :key="index" class="flex flex-wrap items-center gap-2">
            <InputField
              v-model="ingredient.name"
              class="min-w-40 shrink grow basis-1/3"
              type="text"
              :aria-label="rs('IngredientName')"
              :placeholder="rs('IngredientNamePlaceholder')"
            />

            <Button
              :variant="ingredient.isEyeballed ? 'ink' : 'ghost'"
              class="kcc-btn--icon shrink-0"
              :aria-label="rs('Eyeball')"
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
              :aria-label="rs('Quantity')"
              :placeholder="rs('QuantityPlaceholder')"
              :disabled="ingredient.isEyeballed"
            />

            <InputField
              v-model="ingredient.unit"
              class="shrink basis-20"
              type="text"
              :aria-label="rs('Unit')"
              :placeholder="rs('UnitPlaceholder')"
              :disabled="ingredient.isEyeballed"
            />

            <Button
              variant="ghost"
              class="kcc-btn--icon shrink-0"
              :aria-label="rs('Remove')"
              :disabled="ingredientList.length <= 1"
              @click="ingredientList.splice(index, 1)"
            >
              <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
            </Button>
          </li>
        </ul>

        <Button
          class="kcc-btn--icon self-start"
          :aria-label="rs('AddIngredient')"
          @click="ingredientList.push({ name: '', unit: '', isEyeballed: false })"
        >
          <i class="fa-duotone fa-plus" aria-hidden="true"></i>
        </Button>

        <div class="flex flex-wrap justify-between gap-3">
          <Button variant="ghost" @click="step--">
            <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
          </Button>

          <Button type="submit" :disabled="!canProceed">
            <ResourceString for="Next" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
          </Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 3" crisp :tear="3" :pad="STEP_PAD" icon="fa-duotone fa-list-ol">
      <template #label><ResourceString for="Instructions" /></template>

      <h2 class="sr-only">{{ rs('Instructions') }}</h2>

      <form class="flex flex-col gap-6" @submit.prevent="step++">
        <ol class="kcc-steps">
          <li v-for="(instruction, index) in instructionList" :key="index" class="grid-cols-[28px_minmax(0,1fr)_auto]">
            <span class="kcc-n">{{ stepNumber(index) }}</span>

            <TextAreaField v-model="instruction.text" :placeholder="rs('DescribeThisStep')" />

            <Button
              variant="ghost"
              class="kcc-btn--icon"
              :aria-label="rs('Remove')"
              :disabled="instructionList.length <= 1"
              @click="instructionList.splice(index, 1)"
            >
              <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
            </Button>
          </li>
        </ol>

        <Button class="kcc-btn--icon self-start" :aria-label="rs('AddStep')" @click="instructionList.push({ text: '' })">
          <i class="fa-duotone fa-plus" aria-hidden="true"></i>
        </Button>

        <div class="flex flex-wrap justify-between gap-3">
          <Button variant="ghost" @click="step--">
            <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
          </Button>

          <Button type="submit" :disabled="!canProceed">
            <ResourceString for="Next" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
          </Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet v-if="step === 4" crisp :tear="4" :pad="STEP_PAD" icon="fa-duotone fa-clipboard-check">
      <template #label><ResourceString for="ReviewAndSubmit" /></template>

      <h2 class="sr-only">{{ rs('ReviewAndSubmit') }}</h2>

      <div class="space-y-12">
        <div>
          <ResourceString for="VariantInfo" as="p" class="kcc-kick" />
          <h3 class="kcc-h4 mt-6">{{ variantName }}</h3>
          <p v-if="variantDescription" class="kcc-body mt-6">{{ variantDescription }}</p>

          <div v-if="prepTime || cookTime || servings" class="kcc-stats mt-6">
            <div v-if="prepTime">
              <ResourceString for="PrepTime" as="p" class="kcc-lbl" />
              <p class="kcc-v">
                {{ prepTime }}<small><ResourceString for="Min" /></small>
              </p>
            </div>

            <div v-if="cookTime">
              <ResourceString for="CookTime" as="p" class="kcc-lbl" />
              <p class="kcc-v">
                {{ cookTime }}<small><ResourceString for="Min" /></small>
              </p>
            </div>

            <div v-if="servings">
              <ResourceString for="Serves" as="p" class="kcc-lbl" />
              <p class="kcc-v">{{ servings }}</p>
            </div>
          </div>
        </div>

        <div>
          <h4 class="kcc-kick flex items-baseline gap-2">
            <ResourceString for="Ingredients" /><span class="kcc-num">{{ reviewIngredients.length }}</span>
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
            <ResourceString for="Instructions" /><span class="kcc-num">{{ reviewInstructions.length }}</span>
            <ResourceString :for="stepLabelKey(reviewInstructions.length)" />
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
        <Button variant="ghost" @click="step--">
          <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
        </Button>

        <Button :disabled="isSubmitting" @click="handleSubmit">
          <ResourceString :for="isSubmitting ? 'Submitting' : 'SubmitForReview'" />
        </Button>
      </div>
    </KccSheet>
  </section>
</template>
