<script setup lang="ts">
  import { computed } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { ReviewSection, WizardMode } from './recipeWizardFlow'
  import { recipeWizardKeys } from './recipeWizardKeys'
  import ValueMark from './ValueMark.vue'
  import { DIFFICULTIES, NUTRITION_FIELDS, type DetailsChoices, type Difficulty } from './wizardDetails'
  import { fill } from './wizardText'
  import { isFilled, type WizardValues } from './wizardValues'

  export interface DetailsSummaryProps {
    mode: WizardMode
    values: WizardValues
    choices?: DetailsChoices
    disabled?: boolean
  }

  const { mode, values, choices = { categories: [], tags: [] }, disabled = false } = defineProps<DetailsSummaryProps>()

  const emit = defineEmits<{ edit: [section: ReviewSection] }>()

  const rs = useResourceStrings()

  const DIFFICULTY_KEYS: Record<Difficulty, string> = {
    Easy: recipeWizardKeys.DifficultyEasy,
    Medium: recipeWizardKeys.DifficultyMedium,
    Hard: recipeWizardKeys.DifficultyHard,
  }

  const isRecipe = computed(() => mode === 'recipe')

  const categoryName = computed(() => choices.categories.find(({ key }) => key === values.category)?.name ?? '')

  const tags = computed(() =>
    choices.tags
      .filter(({ key }) => values.tags.includes(key))
      .map(({ key, name }) => ({ key, name, mark: values.marks[`variant.tag.${key}`] })),
  )

  const difficulty = computed(() =>
    values.difficulty && DIFFICULTIES.includes(values.difficulty) ? DIFFICULTY_KEYS[values.difficulty] : '',
  )

  const nutrition = computed(() =>
    NUTRITION_FIELDS.map(({ field, label, unit }) => ({
      field,
      label,
      unit,
      value: values.nutrition[field].trim(),
      mark: values.marks[`variant.nutrition.${field}`],
    })).filter(({ value, mark }) => value || mark),
  )

  const showCategory = computed(() => isRecipe.value && Boolean(categoryName.value))
  const showProposedCategory = computed(() => isRecipe.value && isFilled(values.proposedCategory))

  const hasDetails = computed(
    () =>
      showCategory.value ||
      tags.value.length ||
      difficulty.value ||
      values.marks['variant.difficulty'] ||
      nutrition.value.length ||
      showProposedCategory.value ||
      isFilled(values.proposedTags),
  )
</script>

<template>
  <div data-testid="review-details">
    <div class="kcc-row justify-between">
      <ResourceString for="ReviewDetails" as="p" class="kcc-kick" />
      <Button
        variant="text"
        :disabled
        data-testid="review-edit-details"
        :aria-label="fill(rs('EditSection'), rs(recipeWizardKeys.ReviewDetails))"
        @click="emit('edit', 'details')"
      >
        <i class="fa-duotone fa-pen" aria-hidden="true"></i><ResourceString for="Edit" />
      </Button>
    </div>

    <ResourceString v-if="!hasDetails" for="NoDetailsGiven" as="p" class="kcc-body mt-rule-1" />

    <template v-else>
      <div v-if="showCategory" class="mt-rule-1">
        <ResourceString for="Category" as="p" class="kcc-lbl" />
        <p class="kcc-body">{{ categoryName }}</p>
        <ValueMark v-if="values.marks['recipe.category']" :mark="values.marks['recipe.category']" />
      </div>

      <ul v-if="tags.length" class="kcc-check mt-rule-1">
        <li v-for="tag in tags" :key="tag.key" class="grid-cols-[1fr_auto] gap-y-0">
          <span>{{ tag.name }}</span>
          <ValueMark v-if="tag.mark" class="col-span-2" :mark="tag.mark" />
        </li>
      </ul>

      <div v-if="difficulty || values.marks['variant.difficulty']" class="mt-rule-1">
        <ResourceString for="Difficulty" as="p" class="kcc-lbl" />
        <ResourceString v-if="difficulty" :for="difficulty" as="p" class="kcc-body" />
        <ValueMark v-if="values.marks['variant.difficulty']" :mark="values.marks['variant.difficulty']" />
      </div>

      <div v-if="nutrition.length" class="kcc-stats mt-rule-1">
        <div v-for="entry in nutrition" :key="entry.field">
          <ResourceString :for="entry.label" as="p" class="kcc-lbl" />
          <p v-if="entry.value" class="kcc-v h-rule-1">
            {{ entry.value }}<small><ResourceString :for="entry.unit" /></small>
          </p>
          <ValueMark v-if="entry.mark" class="flex-wrap" :mark="entry.mark" />
        </div>
      </div>

      <div v-if="showProposedCategory" class="mt-rule-1">
        <ResourceString for="ProposedCategory" as="p" class="kcc-lbl" />
        <p class="kcc-body">{{ values.proposedCategory }}</p>
      </div>

      <div v-if="isFilled(values.proposedTags)" class="mt-rule-1">
        <ResourceString for="ProposedTags" as="p" class="kcc-lbl" />
        <p class="kcc-body">{{ values.proposedTags }}</p>
      </div>
    </template>
  </div>
</template>
