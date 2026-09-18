<!-- #region RecipesEmptyState Component -->
<script lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  /**
   * Shown when a search returns nothing, offering a way back.
   */
  export default {
    name: 'RecipesEmptyState',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const emit = defineEmits<{ clear: [] }>()
</script>

<template>
  <KccSheet data-testid="recipes-empty" wash="yellow" :at="{ x: '18%', y: '90%', w: '60%', h: '50%' }" :tear="2" tape>
    <div class="grid justify-items-center gap-6 text-center">
      <i class="fa-duotone fa-bowl-food text-5xl text-ink-soft" aria-hidden="true"></i>
      <ResourceString for="NoRecipesMatch" as="p" class="kcc-h4" />
      <ResourceString for="NoRecipesHint" as="p" class="kcc-body max-w-[60ch]" />
      <Button variant="ghost" @click="emit('clear')">
        <ResourceString for="ClearAllFilters" />
      </Button>
    </div>
  </KccSheet>
</template>
