<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'

  export interface ComingSoonSectionProps {
    textKey: string
    tear?: Tear
    shared?: boolean
  }

  const { textKey, tear = 3, shared = true } = defineProps<ComingSoonSectionProps>()
</script>

<template>
  <KccSheet :wash-key="`coming-soon:${textKey}`" wash="lavender" icon="fa-duotone fa-hourglass-half" :tear="tear">
    <template #label><ResourceString :shared="shared" for="ComingSoon" /></template>
    <ResourceString :shared="shared" :for="textKey" as="p" class="kcc-body" />
  </KccSheet>
</template>
