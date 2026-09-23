<!-- #region AppliedFilterChips Component Properties -->
<script lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import type { FilterChip } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  /**
   * Dismissible summary of the filters currently narrowing a search.
   */
  export default {
    name: 'AppliedFilterChips',
  }

  export interface AppliedFilterChipsProps {
    /**
     * Build with `chipsFor` from recipeSearchCriteria; an empty list renders nothing.
     */
    chips: FilterChip[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
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
