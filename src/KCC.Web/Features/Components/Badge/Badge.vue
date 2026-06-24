<script setup lang="ts">
  withDefaults(defineProps<{ color?: string }>(), { color: 'overlay' })

  // 'overlay' (on dark rows) and 'muted' (coming-soon) are special; any other
  // value is treated as a safelisted accent token (e.g. 'peach' -> bg-peach).
  const presets: Record<string, string> = {
    overlay: 'bg-overlay-300 text-bone',
    muted: 'bg-bone-dark text-onyx-light',
  }
</script>

<template>
  <span
    class="inline-flex items-center rounded-full px-3 py-0.5 text-xs font-bold"
    :class="presets[color] ?? `bg-${color} text-onyx`"
  >
    <slot />
  </span>
</template>
