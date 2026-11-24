<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString } from '~/Components/ResourceStrings'
  import { useLaterPartRetry } from '~/Islands/laterParts'

  const retry = useLaterPartRetry()
</script>

<template>
  <ResourceString for="UnexpectedError" as="p" class="kcc-well kcc-well--danger kcc-kick" role="alert" />
  <div class="kcc-row mt-rule-1 justify-end">
    <Button data-test="later-part-retry" @click="retry">
      <ResourceString for="TryAgain" /><i class="fa-duotone fa-rotate-right" aria-hidden="true"></i>
    </Button>
  </div>
</template>
