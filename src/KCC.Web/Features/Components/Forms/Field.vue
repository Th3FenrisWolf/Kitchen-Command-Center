<!-- #region Field Component Properties -->
<script lang="ts">
  /**
   * Label, control, hint and error for one form field. The control goes in the slot and takes
   * `:id="controlId"`; the slot exposes `describedby` for the control's `aria-describedby` and `error` so
   * the slotted control can add `kcc-field--error` to its own pill.
   */
  export default {
    name: 'Field',
  }

  export interface FieldProps {
    /** `label` (string) or the `#label` slot for rich content such as a `<ResourceString>`. */
    label?: string

    /** The control's id; also keys the hint and error ids. */
    controlId: string

    /** `hint` (string) or the `#hint` slot for rich content such as more than one `<ResourceString>`. */
    hint?: string

    error?: string

    required?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
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
