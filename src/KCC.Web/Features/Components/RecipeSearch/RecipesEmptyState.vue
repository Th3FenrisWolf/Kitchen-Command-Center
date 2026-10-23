<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  const emit = defineEmits<{ clear: [] }>()
</script>

<template>
  <KccSheet data-testid="recipes-empty" wash-key="recipes-empty" wash="yellow" bloom="reading" :tear="2" wide>
    <div class="grid justify-items-center gap-y-rule-1 text-center">
      <i class="fa-duotone fa-bowl-food kcc-icon-lg text-ink-soft" aria-hidden="true"></i>
      <ResourceString for="NoRecipesMatch" as="p" class="kcc-title" />
      <ResourceString for="NoRecipesHint" as="p" class="kcc-body max-w-[60ch]" />
      <Button variant="ghost" @click="emit('clear')">
        <ResourceString for="ClearAllFilters" />
      </Button>
    </div>
  </KccSheet>
</template>
