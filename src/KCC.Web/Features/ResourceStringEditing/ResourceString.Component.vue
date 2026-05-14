<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Utilities/UseStrings'

  // Parallel to @Html.ResourceString in MVC: renders the value AND the
  // editor marker (data-resource-key + kcc-rs-editable). Use inside a
  // component that called useResourceStrings(...) so the dict + prefix
  // are available via inject.
  const props = defineProps<{
    k: string
  }>()

  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })

  const fullKey = computed(() => (ctx.prefix ? `${ctx.prefix}.${props.k}` : props.k))
  const value = computed(() => ctx.strings[fullKey.value] ?? props.k)
</script>

<template>
  <span class="kcc-rs-editable" :data-resource-key="fullKey">{{ value }}</span>
</template>
