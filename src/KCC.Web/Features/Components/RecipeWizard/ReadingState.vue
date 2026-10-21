<script setup lang="ts">
  import { onBeforeUnmount, onMounted } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import { useWakeLock } from '~/Pages/VariantDetail/useWakeLock'
  import type { ReadRequest, ReadResult } from './recipeImportApi'
  import { startReadSession, type ReadSession } from './readSession'
  import { STEP_PAD } from './stepSheet'

  export interface ReadingStateProps {
    request: ReadRequest
    tear?: Tear
  }

  const { request, tear } = defineProps<ReadingStateProps>()

  const emit = defineEmits<{ finish: [result: ReadResult] }>()

  const wakeLock = useWakeLock()
  let session: ReadSession | null = null
  let unmounted = false

  onMounted(async () => {
    session = startReadSession(request, wakeLock)
    const result = await session.result
    if (!unmounted) emit('finish', result)
  })

  onBeforeUnmount(() => {
    unmounted = true
    session?.cancel()
  })

  const cancel = () => session?.cancel()
</script>

<template>
  <KccSheet margin crisp wide :tear :pad="STEP_PAD">
    <div class="grid justify-items-center gap-y-rule-1 text-center" aria-busy="true" data-testid="reading-state">
      <h2 class="kcc-kick flex items-center gap-2.5">
        <i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i><ResourceString for="Reading" />
      </h2>

      <Button variant="ghost" data-testid="reading-cancel" @click="cancel">
        <ResourceString for="ReadingCancel" />
      </Button>
    </div>
  </KccSheet>
</template>
