<script setup lang="ts">
  import { computed, ref, useId } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import Field from '~/Components/Forms/Field.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import PossibleDuplicateNotice from './PossibleDuplicateNotice.vue'
  import type { PossibleDuplicate, ReadMatch, RecipeChoice } from './recipeImportApi'
  import { STEP_PAD } from './stepSheet'
  import { fill } from './wizardText'

  export interface MatchStepProps {
    match: ReadMatch
    recipes?: RecipeChoice[] | null
    chosenKey?: string | null
    possibleDuplicate?: PossibleDuplicate | null
    canGoBack?: boolean
    tear?: Tear
  }

  const {
    match,
    recipes = [],
    chosenKey = null,
    possibleDuplicate = null,
    canGoBack = false,
    tear,
  } = defineProps<MatchStepProps>()

  const emit = defineEmits<{ accept: []; pick: [recipe: RecipeChoice]; keep: []; back: [] }>()

  const rs = useResourceStrings()

  const pickId = `${useId()}-recipe`

  const others = computed(() => (recipes ?? []).filter(({ key }) => key !== match.recipeKey))

  const picked = ref(others.value.find(({ key }) => key === chosenKey)?.key ?? others.value[0]?.key ?? '')

  function pick() {
    const recipe = others.value.find(({ key }) => key === picked.value)
    if (recipe) emit('pick', recipe)
  }
</script>

<template>
  <KccSheet crisp wide :tear :pad="STEP_PAD" data-testid="match-step">
    <h2 class="kcc-heading">{{ fill(rs('MatchTitle'), match.recipeName) }}</h2>

    <div class="mt-rule-1 flex flex-col gap-y-rule-1">
      <p v-if="match.reason" class="kcc-body text-ink-soft">{{ match.reason }}</p>

      <PossibleDuplicateNotice v-if="possibleDuplicate" :duplicate="possibleDuplicate" />

      <div class="flex flex-col items-start gap-y-rule-1">
        <Button
          class="h-auto py-rule-0.5 text-left [line-height:var(--bl)] whitespace-normal"
          data-testid="match-accept"
          @click="emit('accept')"
        >
          <i class="fa-duotone fa-layer-group" aria-hidden="true"></i>{{ fill(rs('AddAsVariant'), match.recipeName) }}
        </Button>

        <div v-if="others.length" class="flex w-full flex-wrap items-end gap-x-3 gap-y-rule-1">
          <Field :control-id="pickId" class="min-w-0 flex-1 basis-60">
            <template #label><ResourceString for="PickRecipeLabel" /></template>
            <span class="kcc-field w-full">
              <i class="fa-duotone fa-book-open" aria-hidden="true"></i>
              <select :id="pickId" @change="picked = ($event.target as HTMLSelectElement).value">
                <option v-for="recipe in others" :key="recipe.key" :value="recipe.key" :selected="recipe.key === picked">
                  {{ recipe.name }}
                </option>
              </select>
            </span>
          </Field>

          <div class="flex min-h-rule-2 items-center">
            <Button variant="ghost" data-testid="match-pick" @click="pick">
              <ResourceString for="PickAnotherRecipe" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
            </Button>
          </div>
        </div>

        <Button variant="ghost" data-testid="match-keep" @click="emit('keep')">
          <i class="fa-duotone fa-file-circle-plus" aria-hidden="true"></i><ResourceString for="KeepNewRecipe" />
        </Button>
      </div>

      <div v-if="canGoBack" class="flex flex-wrap justify-between gap-x-3 gap-y-rule-1">
        <Button variant="ghost" @click="emit('back')">
          <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
        </Button>
      </div>
    </div>
  </KccSheet>
</template>
