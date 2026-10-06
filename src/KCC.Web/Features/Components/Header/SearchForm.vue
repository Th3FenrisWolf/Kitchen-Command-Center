<script setup lang="ts">
  export interface SearchFormProps {
    inputId: string
    action: string
    label: string
    controls?: string
    keyHint?: boolean
  }

  const { inputId, action, label, controls, keyHint = false } = defineProps<SearchFormProps>()

  const query = defineModel<string>({ required: true })

  const emit = defineEmits<{ reach: []; keydown: [event: KeyboardEvent] }>()
</script>

<template>
  <form class="pad-search" role="search" :action method="get">
    <label class="kcc-field pad-search-field" :class="{ 'pad-search-field--hint': keyHint }">
      <i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>
      <input
        :id="inputId"
        v-model="query"
        name="query"
        type="text"
        inputmode="search"
        enterkeyhint="search"
        autocomplete="off"
        spellcheck="false"
        :placeholder="label"
        :aria-label="label"
        :aria-controls="controls"
        :aria-keyshortcuts="keyHint ? '/' : undefined"
        @focus="emit('reach')"
        @input="emit('reach')"
        @keydown="(event) => emit('keydown', event)"
      />
      <kbd v-if="keyHint" class="pad-key" aria-hidden="true">/</kbd>
    </label>
  </form>
</template>

<style>
  .pad-search-field--hint {
    grid-template-columns: auto 1fr auto;
  }

  .pad-key {
    border-radius: 4px;
    box-shadow: inset 0 0 0 1px var(--color-hair-strong);
    color: var(--color-ink-soft);
    font: 400 11px/18px var(--font-sono);
    padding: 0 6px;
  }
</style>
