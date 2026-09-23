<!-- #region VariantsEmptyState Component -->
<script lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  /**
   * Shown when a recipe's variants are all filtered out, offering a way back.
   */
  export default {
    name: 'VariantsEmptyState',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const emit = defineEmits<{ clear: [] }>()
</script>

<template>
  <KccSheet wash="sky" :at="{ x: '88%', y: '14%', w: '40%', h: '66%' }" :tear="4" tape>
    <div class="grid justify-items-center gap-6 text-center">
      <i class="fa-duotone fa-bowl-food text-5xl text-ink-soft" aria-hidden="true"></i>
      <ResourceString for="NoVariantsMatch" as="p" class="kcc-h4" />
      <ResourceString for="TryDifferentFilter" as="p" class="kcc-hand" />
      <Button variant="ghost" @click="emit('clear')">
        <ResourceString for="ClearFilters" />
      </Button>
    </div>
  </KccSheet>
</template>
