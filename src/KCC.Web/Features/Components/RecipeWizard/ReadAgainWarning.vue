<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString } from '~/Components/ResourceStrings'

  const { canConfirm } = defineProps<{ canConfirm: boolean }>()

  const emit = defineEmits<{ confirm: []; cancel: [] }>()
</script>

<template>
  <div class="flex flex-col gap-y-rule-1">
    <ResourceString
      for="ReadAgainWarning"
      as="p"
      class="kcc-well kcc-well--warning kcc-kick"
      role="alert"
      data-testid="read-again-warning"
    />

    <div class="kcc-row justify-end">
      <Button variant="ghost" @click="emit('cancel')"><ResourceString for="ReadingCancel" /></Button>
      <Button variant="ghost" :disabled="!canConfirm" data-testid="read-again-confirm" @click="emit('confirm')">
        <ResourceString for="ReadAgain" /><i class="fa-duotone fa-rotate-right" aria-hidden="true"></i>
      </Button>
    </div>
  </div>
</template>
