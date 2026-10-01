<script setup lang="ts">
  export interface FieldProps {
    label?: string
    controlId: string
    hint?: string
    error?: string
    required?: boolean
  }

  const { label, controlId, hint, error, required = false } = defineProps<FieldProps>()
</script>

<template>
  <div class="flex flex-col gap-2">
    <label :for="controlId" class="kcc-lbl">
      <slot name="label">{{ label }}</slot>
      <span v-if="required" aria-hidden="true"> *</span>
    </label>
    <slot
      :describedby="error ? `${controlId}-error` : hint || $slots.hint ? `${controlId}-hint` : undefined"
      :error="!!error"
    />
    <p v-if="error" :id="`${controlId}-error`" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ error }}</p>
    <p v-else-if="hint || $slots.hint" :id="`${controlId}-hint`" class="kcc-kick text-ink">
      <slot name="hint">{{ hint }}</slot>
    </p>
  </div>
</template>
