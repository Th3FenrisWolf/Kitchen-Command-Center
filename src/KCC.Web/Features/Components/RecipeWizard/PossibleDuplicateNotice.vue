<script setup lang="ts">
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import type { PossibleDuplicate } from './recipeImportApi'
  import { fill } from './wizardText'

  const { duplicate } = defineProps<{ duplicate: PossibleDuplicate }>()

  const rs = useResourceStrings()
</script>

<template>
  <p class="kcc-well kcc-well--warning kcc-kick" data-testid="possible-duplicate">
    {{ fill(rs('PossibleDuplicate'), duplicate.variantName, duplicate.recipeName) }}
  </p>
</template>
