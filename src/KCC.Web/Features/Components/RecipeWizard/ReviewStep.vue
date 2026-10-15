<script setup lang="ts">
  import { computed, nextTick, ref, useId, type ComponentPublicInstance } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { Ingredient } from '~/Types/Recipe'
  import PossibleDuplicateNotice from './PossibleDuplicateNotice.vue'
  import ReadAgainWarning from './ReadAgainWarning.vue'
  import type { PossibleDuplicate } from './recipeImportApi'
  import type { ReviewSection, WizardMode } from './recipeWizardFlow'
  import { recipeWizardKeys } from './recipeWizardKeys'
  import { focusHeading, STEP_PAD, stepNumber } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import type { SubmitErrorKey } from './wizardErrors'
  import { fill } from './wizardText'
  import { isFilled, stepLabelKey, validIngredients, validInstructions, type WizardValues } from './wizardValues'

  export interface ReviewStepProps {
    mode: WizardMode
    values: WizardValues
    otherRecipes?: string[]
    possibleDuplicate?: PossibleDuplicate | null
    changesMatch?: boolean
    addingTo?: string | null
    imported?: boolean
    canRead?: boolean
    confirmingRead?: boolean
    tear?: Tear
    submitting?: boolean
    error?: SubmitErrorKey | null
  }

  const {
    mode,
    values,
    otherRecipes = [],
    possibleDuplicate = null,
    changesMatch = false,
    addingTo = null,
    imported = false,
    canRead = false,
    confirmingRead = false,
    tear,
    submitting = false,
    error = null,
  } = defineProps<ReviewStepProps>()

  const emit = defineEmits<{
    back: []
    submit: []
    edit: [section: ReviewSection]
    changeMatch: []
    changePhotos: []
    read: []
    confirmRead: []
    keepEdits: []
  }>()

  const rs = useResourceStrings()

  const readButton = ref<ComponentPublicInstance | null>(null)
  const heading = ref<HTMLElement | null>(null)

  const ingredients = computed(() => validIngredients(values.ingredients))
  const instructions = computed(() => validInstructions(values.instructions))

  const missing = computed(() => ({
    recipeName: mode === 'recipe' && !isFilled(values.recipeName),
    recipeDescription: mode === 'recipe' && !isFilled(values.recipeDescription),
    variantName: !isFilled(values.variantName),
    ingredients: !ingredients.value.length,
    instructions: !instructions.value.length,
  }))

  const complete = computed(() => Object.values(missing.value).every((gap) => !gap))

  const stats = computed(() =>
    [
      {
        label: recipeWizardKeys.PrepTime,
        value: values.prepTime,
        unit: recipeWizardKeys.Min,
        mark: values.marks['variant.prepTime'],
      },
      {
        label: recipeWizardKeys.CookTime,
        value: values.cookTime,
        unit: recipeWizardKeys.Min,
        mark: values.marks['variant.cookTime'],
      },
      { label: recipeWizardKeys.Serves, value: values.servings, mark: values.marks['variant.servings'] },
    ].filter(({ value, mark }) => value || mark),
  )

  const otherRecipesSeen = computed(() =>
    otherRecipes.length ? fill(rs('OtherRecipesSeen'), otherRecipes.join(', ')) : null,
  )

  const matchLineId = `${useId()}-match`

  const matchLine = computed(() => (addingTo ? fill(rs('AddingTo'), addingTo) : rs('KeepingNewRecipe')))

  const variantTitle = computed(() =>
    mode === 'recipe' ? recipeWizardKeys.ReviewFirstVariant : recipeWizardKeys.VariantInfo,
  )

  const editLabel = (titleKey: string) => fill(rs('EditSection'), rs(titleKey))

  async function keepEdits() {
    emit('keepEdits')
    await nextTick()
    const asker: HTMLButtonElement | undefined = readButton.value?.$el
    if (asker && !asker.disabled) asker.focus()
    else focusHeading(heading.value)
  }

  const amount = (ingredient: Ingredient) =>
    !ingredient.isEyeballed && ingredient.quantity ? `${ingredient.quantity} ${ingredient.unit}`.trim() : rs('ToTaste')
</script>

<template>
  <KccSheet crisp :tear :pad="STEP_PAD" icon="fa-duotone fa-clipboard-check" data-testid="review-step">
    <template #label><ResourceString for="ReviewAndSubmit" /></template>

    <h2 ref="heading" class="sr-only">{{ rs('ReviewAndSubmit') }}</h2>

    <div class="space-y-12" :aria-busy="submitting">
      <div v-if="changesMatch" class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
        <p :id="matchLineId" class="kcc-h4">{{ matchLine }}</p>
        <Button variant="text" :disabled="submitting" :aria-describedby="matchLineId" @click="emit('changeMatch')">
          <i class="fa-duotone fa-arrows-rotate" aria-hidden="true"></i><ResourceString for="ChangeMatch" />
        </Button>
      </div>

      <PossibleDuplicateNotice v-if="possibleDuplicate" :duplicate="possibleDuplicate" />

      <p v-if="otherRecipesSeen" class="kcc-well kcc-kick">{{ otherRecipesSeen }}</p>

      <div v-if="mode === 'recipe'">
        <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
          <ResourceString for="ReviewRecipe" as="p" class="kcc-kick" />
          <Button
            variant="text"
            :disabled="submitting"
            data-testid="review-edit-basics"
            :aria-label="editLabel(recipeWizardKeys.ReviewRecipe)"
            @click="emit('edit', 'basics')"
          >
            <i class="fa-duotone fa-pen" aria-hidden="true"></i><ResourceString for="Edit" />
          </Button>
        </div>
        <ResourceString
          v-if="missing.recipeName"
          for="MissingValue"
          as="p"
          class="kcc-well kcc-well--warning kcc-kick mt-6"
        />
        <h3 v-else class="kcc-h4 mt-6">{{ values.recipeName }}</h3>
        <ValueMark v-if="values.marks['recipe.name']" :mark="values.marks['recipe.name']" />

        <ResourceString
          v-if="missing.recipeDescription"
          for="MissingValue"
          as="p"
          class="kcc-well kcc-well--warning kcc-kick mt-6"
        />
        <p v-else class="kcc-body mt-6">{{ values.recipeDescription }}</p>
        <ValueMark v-if="values.marks['recipe.description']" :mark="values.marks['recipe.description']" />
      </div>

      <div>
        <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
          <ResourceString :for="variantTitle" as="p" class="kcc-kick" />
          <Button
            variant="text"
            :disabled="submitting"
            data-testid="review-edit-variant"
            :aria-label="editLabel(variantTitle)"
            @click="emit('edit', 'variant')"
          >
            <i class="fa-duotone fa-pen" aria-hidden="true"></i><ResourceString for="Edit" />
          </Button>
        </div>
        <ResourceString
          v-if="missing.variantName"
          for="MissingValue"
          as="p"
          class="kcc-well kcc-well--warning kcc-kick mt-6"
        />
        <h3 v-else class="kcc-h4 mt-6">{{ values.variantName }}</h3>
        <ValueMark v-if="values.marks['variant.name']" :mark="values.marks['variant.name']" />

        <p v-if="values.variantDescription" class="kcc-body mt-6">{{ values.variantDescription }}</p>
        <ValueMark v-if="values.marks['variant.description']" :mark="values.marks['variant.description']" />

        <div v-if="stats.length" class="kcc-stats mt-6">
          <div v-for="stat in stats" :key="stat.label">
            <ResourceString :for="stat.label" as="p" class="kcc-lbl" />
            <p v-if="stat.value" class="kcc-v h-6">
              {{ stat.value }}<small v-if="stat.unit"><ResourceString :for="stat.unit" /></small>
            </p>
            <ValueMark v-if="stat.mark" class="flex-wrap" :mark="stat.mark" />
          </div>
        </div>
      </div>

      <div>
        <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
          <h4 class="kcc-kick flex items-baseline gap-2">
            <ResourceString for="Ingredients" /><span class="kcc-num">{{ ingredients.length }}</span>
          </h4>
          <Button
            variant="text"
            :disabled="submitting"
            data-testid="review-edit-ingredients"
            :aria-label="editLabel(recipeWizardKeys.Ingredients)"
            @click="emit('edit', 'ingredients')"
          >
            <i class="fa-duotone fa-pen" aria-hidden="true"></i><ResourceString for="Edit" />
          </Button>
        </div>

        <ResourceString
          v-if="missing.ingredients"
          for="MissingValue"
          as="p"
          class="kcc-well kcc-well--warning kcc-kick mt-6"
        />
        <ul v-else class="kcc-check mt-6">
          <li v-for="(ingredient, index) in ingredients" :key="index" class="grid-cols-[1fr_auto] gap-y-0">
            <span>{{ ingredient.name }}</span>
            <span class="kcc-q">{{ amount(ingredient) }}</span>
            <ValueMark v-if="ingredient.mark" class="col-span-2" :mark="ingredient.mark" />
          </li>
        </ul>
      </div>

      <div>
        <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
          <h4 class="kcc-kick flex items-baseline gap-2">
            <ResourceString for="Instructions" /><span class="kcc-num">{{ instructions.length }}</span>
            <ResourceString :for="stepLabelKey(instructions.length)" />
          </h4>
          <Button
            variant="text"
            :disabled="submitting"
            data-testid="review-edit-instructions"
            :aria-label="editLabel(recipeWizardKeys.Instructions)"
            @click="emit('edit', 'instructions')"
          >
            <i class="fa-duotone fa-pen" aria-hidden="true"></i><ResourceString for="Edit" />
          </Button>
        </div>

        <ResourceString
          v-if="missing.instructions"
          for="MissingValue"
          as="p"
          class="kcc-well kcc-well--warning kcc-kick mt-6"
        />
        <ol v-else class="kcc-steps mt-6">
          <li v-for="(instruction, index) in instructions" :key="index" class="gap-y-0">
            <span class="kcc-n">{{ stepNumber(index) }}</span>
            <p class="kcc-body">{{ instruction.text }}</p>
            <ValueMark v-if="instruction.mark" class="col-start-2" :mark="instruction.mark" />
          </li>
        </ol>
      </div>
    </div>

    <div v-if="imported" class="mt-12 flex flex-col gap-6">
      <div class="flex flex-wrap gap-x-3 gap-y-6">
        <Button variant="ghost" :disabled="submitting" data-testid="change-photos" @click="emit('changePhotos')">
          <i class="fa-duotone fa-images" aria-hidden="true"></i><ResourceString for="ChangePhotos" />
        </Button>
        <Button
          variant="ghost"
          :disabled="submitting || !canRead"
          ref="readButton"
          data-testid="read-again"
          @click="emit('read')"
        >
          <i class="fa-duotone fa-rotate-right" aria-hidden="true"></i><ResourceString for="ReadAgain" />
        </Button>
      </div>

      <ReadAgainWarning
        v-if="confirmingRead"
        :can-confirm="canRead && !submitting"
        @confirm="emit('confirmRead')"
        @cancel="keepEdits"
      />
    </div>

    <p v-if="error" class="kcc-well kcc-well--danger kcc-kick mt-6" role="alert" data-testid="submit-error">
      {{ rs(error) }}
    </p>

    <div class="mt-12 flex flex-wrap justify-between gap-x-3 gap-y-6">
      <Button variant="ghost" :disabled="submitting" @click="emit('back')">
        <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
      </Button>

      <Button :disabled="submitting || !complete" data-testid="wizard-submit" @click="emit('submit')">
        <template v-if="submitting">
          <i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i><ResourceString for="Submitting" />
        </template>
        <ResourceString v-else for="SubmitForReview" />
      </Button>
    </div>
  </KccSheet>
</template>
