<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import type { FilterChip } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  export interface AppliedFilterChipsProps {
    /**
     * Build with `chipsFor` from recipeSearchCriteria; an empty list renders nothing.
     */
    chips: FilterChip[]
  }

  defineProps<AppliedFilterChipsProps>()
  const emit = defineEmits<{ remove: [FilterChip]; clearAll: [] }>()
</script>

<template>
  <div v-if="chips.length" class="kcc-badges mb-6 items-center">
    <button
      v-for="(chip, i) in chips"
      :key="`${chip.kind}-${chip.value ?? i}`"
      class="kcc-badge cursor-pointer gap-2"
      @click="emit('remove', chip)"
    >
      {{ chip.label }} <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
    </button>
    <Button variant="text" @click="emit('clearAll')">
      <ResourceString for="ClearAll" />
    </Button>
  </div>
</template>
