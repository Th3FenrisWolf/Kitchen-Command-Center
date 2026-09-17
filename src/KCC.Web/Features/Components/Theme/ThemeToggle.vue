<!-- #region ThemeToggle Component Properties -->
<script lang="ts">
  /**
   * Two-state ramp switch for the header utility nav. Clicking it opts out of `prefers-color-scheme`
   * permanently, which is what an explicit toggle is expected to do.
   */
  export default {
    name: 'ThemeToggle',
  }

  export type Ramp = 'light' | 'dark'

  export interface ThemeToggleProps {
    /** Announced while the dark ramp is active, when clicking switches to light. */
    switchToLightLabel: string

    /** Announced while the light ramp is active, when clicking switches to dark. */
    switchToDarkLabel: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const STORAGE_KEY = 'kcc-theme'

  const { switchToLightLabel, switchToDarkLabel } = defineProps<ThemeToggleProps>()

  // Both glyphs render and Kit.css shows the one for the active ramp. The server cannot know the ramp, so
  // deciding here would be a hydration mismatch on every dark-ramp visitor.
  function toggle() {
    const root = document.documentElement
    // Light is the default, so a missing attribute flips to dark.
    const next: Ramp = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark'

    // Kill in-flight transitions for one frame so nothing interpolates between the two palettes.
    root.setAttribute('data-theme-switching', '')
    root.setAttribute('data-theme', next)
    try {
      localStorage.setItem(STORAGE_KEY, next)
    } catch {
      // Storage blocked: the ramp still switches for this page view.
    }
    requestAnimationFrame(() => root.removeAttribute('data-theme-switching'))
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
