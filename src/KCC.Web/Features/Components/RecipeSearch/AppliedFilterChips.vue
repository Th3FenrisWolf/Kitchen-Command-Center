<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import type { FilterChip } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  defineProps<{ chips: FilterChip[] }>()
  const emit = defineEmits<{ remove: [FilterChip]; clearAll: [] }>()
</script>

<template>
  <div v-if="chips.length" class="mb-4 flex flex-wrap items-center gap-2">
    <button
      v-for="(chip, i) in chips"
      :key="`${chip.kind}-${chip.value ?? i}`"
      class="inline-flex items-center gap-2 rounded-full border-2 border-onyx bg-onyx px-2 py-1 text-sm font-bold text-bone"
      @click="emit('remove', chip)"
    >
      {{ chip.label }} <i class="fa-solid fa-xmark text-xs"></i>
    </button>
    <button class="text-sm font-bold text-onyx-light underline" @click="emit('clearAll')">
      <ResourceString for="ClearAll" />
    </button>
  </div>
</template>
