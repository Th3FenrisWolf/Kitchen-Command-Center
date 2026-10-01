<script setup lang="ts">
  import type { Instruction } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  export interface VariantInstructionsProps {
    instructions: Instruction[]
  }

  defineProps<VariantInstructionsProps>()

  const rs = useResourceStrings()

  const stepNumber = (instruction: Instruction, index: number) => String(instruction.step ?? index + 1).padStart(2, '0')
</script>

<template>
  <KccSheet
    as="section"
    icon="fa-duotone fa-list-ol"
    wash="peach"
    :at="{ x: '90%', y: '10%', w: '40%', h: '50%' }"
    :tear="6"
  >
    <template #label><ResourceString for="Instructions" /></template>

    <h2 class="sr-only">{{ rs('Instructions') }}</h2>

    <p class="kcc-kick">
      <span class="kcc-num">{{ instructions.length }}</span> steps
    </p>

    <ol class="kcc-steps mt-6">
      <li v-for="(instruction, i) in instructions" :key="i">
        <span class="kcc-n">{{ stepNumber(instruction, i) }}</span>
        <p class="kcc-body">{{ instruction.text }}</p>
      </li>
    </ol>
  </KccSheet>
</template>
