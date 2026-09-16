<!-- #region VariantInstructions Component Properties -->
<script lang="ts">
  import type { Instruction } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'

  /**
   * Numbered walkthrough of a variant's steps.
   */
  export default {
    name: 'VariantInstructions',
  }

  export interface VariantInstructionsProps {
    instructions: Instruction[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<VariantInstructionsProps>()
</script>

<template>
  <div>
    <h2 class="mb-4 font-casual text-2xl tracking-[1px]"><ResourceString for="Instructions" /></h2>
    <ol class="flex flex-col gap-3">
      <li v-for="(instruction, i) in instructions" :key="i" class="flex gap-4 rounded-2xl bg-paper-2 p-4 text-ink">
        <span class="w-8 flex-none text-center font-casual text-3xl leading-none text-ink-soft">
          {{ instruction.step ?? i + 1 }}
        </span>
        <p class="min-w-0 flex-1 text-lg leading-normal">{{ instruction.text }}</p>
      </li>
    </ol>
  </div>
</template>
