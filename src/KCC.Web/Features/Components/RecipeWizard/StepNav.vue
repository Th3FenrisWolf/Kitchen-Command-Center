<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString } from '~/Components/ResourceStrings'

  export interface StepNavProps {
    canGoBack: boolean
    canProceed: boolean
    returnsToReview?: boolean
  }

  const { canGoBack, canProceed, returnsToReview = false } = defineProps<StepNavProps>()

  const emit = defineEmits<{ back: []; review: [] }>()
</script>

<template>
  <div class="flex flex-wrap justify-between gap-x-3 gap-y-6">
    <Button v-if="canGoBack" variant="ghost" @click="emit('back')">
      <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
    </Button>

    <div class="ml-auto flex flex-wrap justify-end gap-x-3 gap-y-6">
      <Button v-if="returnsToReview" variant="ghost" data-testid="back-to-review" @click="emit('review')">
        <i class="fa-duotone fa-clipboard-check" aria-hidden="true"></i><ResourceString for="BackToReview" />
      </Button>

      <Button type="submit" :disabled="!canProceed">
        <ResourceString for="Next" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
      </Button>
    </div>
  </div>
</template>
