<script setup lang="ts">
  import { computed, useId } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import { STEP_PAD } from './stepSheet'
  import { fill, longDay } from './wizardText'

  export interface StartStepProps {
    availableAgainOn?: string | null
    tear?: Tear
  }

  const { availableAgainOn = null, tear } = defineProps<StartStepProps>()

  const rs = useResourceStrings()

  const emit = defineEmits<{ photos: []; typing: [] }>()

  const uid = useId()
  const hints = {
    photos: `${uid}-photos-hint`,
    typing: `${uid}-typing-hint`,
  }
  const noticeId = `${uid}-unavailable`
  const photosDescribedBy = computed(() => (availableAgainOn ? `${hints.photos} ${noticeId}` : hints.photos))
</script>

<template>
  <KccSheet crisp :tear :pad="STEP_PAD">
    <ResourceString for="StartTitle" as="h2" class="kcc-h4" />

    <div class="mt-6 grid gap-x-7 gap-y-6 sm:grid-cols-2">
      <div class="flex flex-col items-start gap-6">
        <Button
          size="lg"
          data-testid="wizard-start-photos"
          :aria-describedby="photosDescribedBy"
          :disabled="availableAgainOn !== null"
          @click="emit('photos')"
        >
          <i class="fa-duotone fa-camera" aria-hidden="true"></i><ResourceString for="StartFromPhotos" />
        </Button>
        <ResourceString :id="hints.photos" for="StartFromPhotosHint" as="p" class="kcc-body text-ink-soft" />
        <p v-if="availableAgainOn" :id="noticeId" class="kcc-well kcc-kick" data-testid="import-unavailable">
          {{ fill(rs('ImportUnavailable'), longDay(availableAgainOn)) }}
        </p>
      </div>

      <div class="flex flex-col items-start gap-6">
        <Button
          variant="ghost"
          size="lg"
          data-testid="wizard-start-typing"
          :aria-describedby="hints.typing"
          @click="emit('typing')"
        >
          <i class="fa-duotone fa-keyboard" aria-hidden="true"></i><ResourceString for="StartTyping" />
        </Button>
        <ResourceString :id="hints.typing" for="StartTypingHint" as="p" class="kcc-body text-ink-soft" />
      </div>
    </div>
  </KccSheet>
</template>
