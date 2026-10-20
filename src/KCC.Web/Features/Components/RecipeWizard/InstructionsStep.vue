<script setup lang="ts">
  import { useId } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import StepNav from './StepNav.vue'
  import { markId, STEP_PAD, stepNumber } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import type { WizardValues } from './wizardValues'

  export interface InstructionsStepProps {
    values: WizardValues
    tear?: Tear
    canGoBack?: boolean
    canProceed?: boolean
    returnsToReview?: boolean
  }

  const {
    values,
    tear,
    canGoBack = false,
    canProceed = false,
    returnsToReview = false,
  } = defineProps<InstructionsStepProps>()

  const emit = defineEmits<{
    edit: [index: number, text: string]
    add: []
    remove: [index: number]
    next: []
    back: []
    review: []
  }>()

  const rs = useResourceStrings()

  const uid = useId()
  const textId = (index: number) => `${uid}-instruction-${index}`
</script>

<template>
  <KccSheet crisp wide :tear :pad="STEP_PAD" icon="fa-duotone fa-list-ol">
    <template #label><ResourceString for="Instructions" /></template>

    <h2 class="sr-only">{{ rs('Instructions') }}</h2>

    <form class="flex flex-col gap-y-rule-1" @submit.prevent="emit('next')">
      <ol class="kcc-steps">
        <li
          v-for="(instruction, index) in values.instructions"
          :key="index"
          class="grid-cols-[28px_minmax(0,1fr)_auto] gap-y-0"
        >
          <span class="kcc-n">{{ stepNumber(index) }}</span>

          <TextAreaField
            :id="textId(index)"
            :model-value="instruction.text"
            data-testid="wizard-instruction-text"
            :aria-label="rs('DescribeThisStep')"
            :placeholder="rs('DescribeThisStep')"
            :aria-describedby="instruction.mark && markId(textId(index))"
            @update:model-value="(text) => emit('edit', index, text)"
          />

          <Button
            variant="ghost"
            class="kcc-btn--icon"
            :aria-label="rs('RemoveStep')"
            :disabled="values.instructions.length <= 1"
            @click="emit('remove', index)"
          >
            <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
          </Button>

          <ValueMark v-if="instruction.mark" :id="markId(textId(index))" class="col-start-2" :mark="instruction.mark" />
        </li>
      </ol>

      <Button variant="ghost" class="self-start" @click="emit('add')">
        <i class="fa-duotone fa-plus" aria-hidden="true"></i><ResourceString for="AddStep" />
      </Button>

      <StepNav :can-go-back :can-proceed :returns-to-review @back="emit('back')" @review="emit('review')" />
    </form>
  </KccSheet>
</template>
