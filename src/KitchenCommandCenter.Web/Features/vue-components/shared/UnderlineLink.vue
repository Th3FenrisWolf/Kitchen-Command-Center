<script setup lang="ts">
import { computed } from 'vue'
import cx from '~/Utilities/CX'

const { color, linkTo } = defineProps<{ color: string; linkTo: string }>()

// Normalize URLs: convert ASP.NET ~/path format to /path for consistency
// This prevents hydration mismatches between SSR and client
const normalizedHref = computed(() => {
  if (linkTo?.startsWith('~/')) {
    return linkTo.slice(1) // Remove the ~ prefix, keep the /
  }
  return linkTo
})
</script>

<template>
  <div class="group/fill-up-link relative my-1 inline-block">
    <a :href="normalizedHref" class="peer focus:outline-0">
      <slot />
    </a>
    <div
      :class="
        cx(
          'absolute right-0 bottom-0 left-1/2 h-1 w-0 rounded-full transition-all group-hover/fill-up-link:left-0 group-hover/fill-up-link:w-full peer-focus:left-0 peer-focus:w-full',
          color,
        )
      "
    />
  </div>
</template>
