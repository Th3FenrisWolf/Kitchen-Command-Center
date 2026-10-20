<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  const emit = defineEmits<{ clear: [] }>()
</script>

<template>
  <KccSheet wash="sky" :at="{ x: '88%', y: '14%', w: '40%', h: '66%' }" :tear="4" wide>
    <div class="grid justify-items-center gap-y-rule-1 text-center">
      <i class="fa-duotone fa-bowl-food kcc-icon-lg text-ink-soft" aria-hidden="true"></i>
      <ResourceString for="NoVariantsMatch" as="p" class="kcc-title" />
      <ResourceString for="TryDifferentFilter" as="p" class="kcc-hand" />
      <Button variant="ghost" @click="emit('clear')">
        <ResourceString for="ClearFilters" />
      </Button>
    </div>
  </KccSheet>
</template>
