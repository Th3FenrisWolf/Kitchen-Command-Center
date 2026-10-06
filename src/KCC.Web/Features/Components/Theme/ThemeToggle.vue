<script setup lang="ts">
  import { STORED_RAMP_KEY, applyRamp, type Ramp } from '~/Utilities/Ramp'

  export interface ThemeToggleProps {
    switchToLightLabel: string
    switchToDarkLabel: string
  }

  const { switchToLightLabel, switchToDarkLabel } = defineProps<ThemeToggleProps>()

  // Both glyphs render and Kit.css shows the one for the active ramp. The server cannot always know the ramp, so
  // deciding here would be a hydration mismatch on some dark-ramp visitors.
  function toggle() {
    // Light is the default, so a missing attribute flips to dark.
    const next: Ramp = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark'

    applyRamp(next)
    try {
      localStorage.setItem(STORED_RAMP_KEY, next)
    } catch {
      // Storage blocked: the ramp still switches for this page view.
    }
  }
</script>

<template>
  <button
    type="button"
    class="btn-no-style cursor-pointer px-2 py-2 text-ink focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ink"
    @click="toggle"
  >
    <span data-ramp="dark">
      <i class="fa-duotone fa-sun-bright text-xl" aria-hidden="true"></i>
      <span class="sr-only">{{ switchToLightLabel }}</span>
    </span>
    <span data-ramp="light">
      <i class="fa-duotone fa-moon text-xl" aria-hidden="true"></i>
      <span class="sr-only">{{ switchToDarkLabel }}</span>
    </span>
  </button>
</template>
