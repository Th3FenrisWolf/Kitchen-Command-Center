<script setup lang="ts">
  import { computed, useId } from 'vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import SelectField from '~/Components/Forms/SelectField.vue'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { ProposalField, ValueEdit, WizardMode } from './recipeWizardFlow'
  import { recipeWizardKeys } from './recipeWizardKeys'
  import StepNav from './StepNav.vue'
  import { describedBy, markId, STEP_PAD } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import {
    DIFFICULTIES,
    NUTRITION_FIELDS,
    nutritionState,
    type DetailsChoices,
    type Difficulty,
    type NutritionField,
    type TagKind,
  } from './wizardDetails'
  import { fill } from './wizardText'
  import type { WizardValues } from './wizardValues'

  export interface DetailsStepProps {
    mode: WizardMode
    values: WizardValues
    choices: DetailsChoices
    tear?: Tear
    canGoBack?: boolean
    canProceed?: boolean
    returnsToReview?: boolean
    detailsChanged?: boolean
  }

  const {
    mode,
    values,
    choices,
    tear,
    canGoBack = false,
    canProceed = false,
    returnsToReview = false,
    detailsChanged = false,
  } = defineProps<DetailsStepProps>()

  const emit = defineEmits<{
    edit: ValueEdit<'category' | 'difficulty'>
    tag: [key: string, chosen: boolean]
    nutrition: [field: NutritionField, text: string]
    proposal: [field: ProposalField, text: string]
    next: []
    back: []
    review: []
  }>()

  const rs = useResourceStrings()

  const uid = useId()
  const ids = {
    category: `${uid}-category`,
    difficulty: `${uid}-difficulty`,
    proposedCategory: `${uid}-proposed-category`,
    proposedTags: `${uid}-proposed-tags`,
    proposalHint: `${uid}-proposal-hint`,
  }
  const tagId = (key: string) => `${uid}-tag-${key}`
  const nutritionId = (field: NutritionField) => `${uid}-nutrition-${field}`

  const TAG_GROUPS: { kind: TagKind; legend: string }[] = [
    { kind: 'Diet', legend: recipeWizardKeys.DietTags },
    { kind: 'Style', legend: recipeWizardKeys.StyleTags },
  ]

  // A select fires no change when its selected option is chosen again, yet choosing the suggested category must clear
  // its mark. While marked, the suggestion shows on a hidden copy, so its listed option is a different one to choose.
  const suggestedCategory = computed(() => (values.marks['recipe.category'] ? values.category : null))

  const tagGroups = computed(() =>
    TAG_GROUPS.map((group) => ({ ...group, tags: choices.tags.filter(({ kind }) => kind === group.kind) })).filter(
      ({ tags }) => tags.length > 0,
    ),
  )

  const difficulties = computed((): SegmentOption<'' | Difficulty>[] => [
    { value: '', label: rs('DifficultyNone') },
    ...DIFFICULTIES.map((level) => ({ value: level, label: rs(`Difficulty${level}`) })),
  ])

  const nutrition = computed(() =>
    NUTRITION_FIELDS.map(({ field, label, unit, limit }) => ({
      field,
      label,
      unit: rs(unit),
      id: nutritionId(field),
      error:
        nutritionState(field, values.nutrition[field]) === 'invalid'
          ? fill(rs('NutritionInvalid'), String(limit))
          : undefined,
    })),
  )

  const nutritionValid = computed(() => nutrition.value.every(({ error }) => error === undefined))

  const proposals = computed(() =>
    (mode === 'recipe' ? (['proposedCategory', 'proposedTags'] as const) : (['proposedTags'] as const)).map((field) => ({
      field,
      id: ids[field],
      hook: field === 'proposedCategory' ? 'details-proposed-category' : 'details-proposed-tags',
      label: field === 'proposedCategory' ? recipeWizardKeys.ProposedCategory : recipeWizardKeys.ProposedTags,
    })),
  )
</script>

<template>
  <KccSheet
    wash-key="wizard-details"
    margin
    crisp
    wide
    :tear
    :pad="STEP_PAD"
    icon="fa-duotone fa-tags"
    data-testid="details-step"
  >
    <template #label><ResourceString for="Details" /></template>

    <h2 class="sr-only">{{ rs('Details') }}</h2>

    <form class="flex flex-col gap-y-rule-1" @submit.prevent="emit('next')">
      <ResourceString
        v-if="detailsChanged"
        for="DetailsChanged"
        as="p"
        class="kcc-well kcc-well--warning kcc-kick"
        role="alert"
        data-testid="details-changed"
      />

      <ResourceString for="DetailsHint" as="p" class="kcc-body text-ink-soft" />

      <Field v-if="mode === 'recipe'" :control-id="ids.category">
        <template #label><ResourceString for="Category" /></template>
        <SelectField
          :id="ids.category"
          data-testid="details-category"
          :aria-describedby="values.marks['recipe.category'] && markId(ids.category)"
          @change="emit('edit', 'category', ($event.target as HTMLSelectElement).value || null)"
        >
          <option value="" :selected="values.category === null">{{ rs('NoCategory') }}</option>
          <template v-for="category in choices.categories" :key="category.key">
            <option v-if="category.key === suggestedCategory" :value="category.key" selected hidden disabled>
              {{ category.name }}
            </option>
            <option :value="category.key" :selected="category.key === values.category && !suggestedCategory">
              {{ category.name }}
            </option>
          </template>
        </SelectField>
        <template #after>
          <ValueMark
            v-if="values.marks['recipe.category']"
            :id="markId(ids.category)"
            :mark="values.marks['recipe.category']"
          />
        </template>
      </Field>

      <div class="grid gap-x-7 gap-y-rule-1 md:grid-cols-2">
        <fieldset v-for="group in tagGroups" :key="group.kind">
          <ResourceString :for="group.legend" as="legend" class="kcc-kick" />
          <ul class="kcc-check">
            <li v-for="tag in group.tags" :key="tag.key" class="cursor-pointer gap-y-0">
              <input
                :id="tagId(tag.key)"
                type="checkbox"
                class="sr-only"
                :checked="values.tags.includes(tag.key)"
                :data-testid="`details-tag-${tag.key}`"
                :aria-describedby="values.marks[`variant.tag.${tag.key}`] && markId(tagId(tag.key))"
                @change="emit('tag', tag.key, ($event.target as HTMLInputElement).checked)"
              />
              <label :for="tagId(tag.key)" class="kcc-box" :class="{ 'kcc-box--on': values.tags.includes(tag.key) }"></label>
              <label :for="tagId(tag.key)">{{ tag.name }}</label>
              <ValueMark
                v-if="values.marks[`variant.tag.${tag.key}`]"
                :id="markId(tagId(tag.key))"
                class="col-span-2 col-start-2"
                :mark="values.marks[`variant.tag.${tag.key}`]!"
              />
            </li>
          </ul>
        </fieldset>
      </div>

      <div class="flex flex-col">
        <ResourceString :id="ids.difficulty" for="Difficulty" class="kcc-lbl" />
        <div class="kcc-row">
          <SegmentedControl
            :model-value="values.difficulty ?? ''"
            :options="difficulties"
            data-testid="details-difficulty"
            :aria-labelledby="ids.difficulty"
            :aria-describedby="values.marks['variant.difficulty'] && markId(ids.difficulty)"
            @pick="(level) => emit('edit', 'difficulty', level || null)"
          />
        </div>
        <ValueMark
          v-if="values.marks['variant.difficulty']"
          :id="markId(ids.difficulty)"
          :mark="values.marks['variant.difficulty']"
        />
      </div>

      <fieldset>
        <ResourceString for="NutritionPerServing" as="legend" class="kcc-kick" />
        <div class="grid gap-x-7 gap-y-rule-1 sm:grid-cols-2 lg:grid-cols-4">
          <Field v-for="row in nutrition" :key="row.field" :control-id="row.id" :error="row.error">
            <template #label><ResourceString :for="row.label" /></template>
            <template #default="{ describedby, error }">
              <div class="flex items-start gap-x-2">
                <InputField
                  :id="row.id"
                  :model-value="values.nutrition[row.field]"
                  :data-testid="`details-nutrition-${row.field}`"
                  class="min-w-0 flex-1"
                  input-class="kcc-num"
                  type="text"
                  inputmode="numeric"
                  :aria-label="fill(rs('ValueInUnit'), rs(row.label), row.unit)"
                  :error
                  :aria-describedby="
                    describedBy(describedby, values.marks[`variant.nutrition.${row.field}`] && markId(row.id))
                  "
                  @update:model-value="(text: string) => emit('nutrition', row.field, text)"
                />
                <span class="kcc-unit shrink-0">{{ row.unit }}</span>
              </div>
            </template>
            <template #after>
              <ValueMark
                v-if="values.marks[`variant.nutrition.${row.field}`]"
                :id="markId(row.id)"
                :mark="values.marks[`variant.nutrition.${row.field}`]!"
              />
            </template>
          </Field>
        </div>
      </fieldset>

      <div class="flex flex-col">
        <div class="grid gap-x-7 gap-y-rule-1 md:grid-cols-2">
          <Field v-for="proposal in proposals" :key="proposal.field" :control-id="proposal.id">
            <template #label><ResourceString :for="proposal.label" /></template>
            <InputField
              :id="proposal.id"
              :model-value="values[proposal.field]"
              :data-testid="proposal.hook"
              type="text"
              maxlength="100"
              :aria-describedby="ids.proposalHint"
              @update:model-value="(text: string) => emit('proposal', proposal.field, text)"
            />
          </Field>
        </div>
        <ResourceString :id="ids.proposalHint" for="ProposalHint" as="p" class="kcc-body text-ink-soft" />
      </div>

      <StepNav
        :can-go-back
        :can-proceed="canProceed && nutritionValid"
        :returns-to-review
        :can-review="nutritionValid"
        @back="emit('back')"
        @review="emit('review')"
      />
    </form>
  </KccSheet>
</template>
