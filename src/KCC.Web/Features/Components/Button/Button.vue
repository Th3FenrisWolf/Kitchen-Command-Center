<!-- #region Button Component Properties -->
<script lang="ts">
  /**
   * Drawn pill button. `marker` is the primary action, `paper` and `ghost` are secondary, `text` is a link.
   * Razor gets the same look from the `.sk-btn` classes plus `data-ink="button"`.
   */
  export default {
    // The rule guards against a global registration shadowing <button>; this one is imported locally.
    // eslint-disable-next-line vue/no-reserved-component-names
    name: 'Button',
  }

  export type ButtonVariant = 'marker' | 'paper' | 'ghost' | 'text'
  export type ButtonSize = 'sm' | 'md' | 'lg'

  export interface ButtonProps {
    /**
     * @default 'marker'
     */
    variant?: ButtonVariant

    /**
     * @default 'md'
     */
    size?: ButtonSize

    /**
     * Render as an anchor by passing `'a'` with an `href`.
     * @default 'button'
     */
    as?: 'button' | 'a'

    /**
     * @default 'button'
     */
    type?: 'button' | 'submit' | 'reset'

    href?: string

    disabled?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { computed } from 'vue'

  const {
    variant = 'marker',
    size = 'md',
    as = 'button',
    type = 'button',
    href,
    disabled = false,
  } = defineProps<ButtonProps>()

  // Filled pills take the fixed ink that sits on washes; unfilled ones are drawn in the ramp's ink line.
  const inkBinding = computed(() =>
    variant === 'text'
      ? null
      : { kind: 'button' as const, color: variant === 'marker' ? undefined : 'var(--color-ink-line)' },
  )
</script>

<template>
  <component
    :is="as"
    v-ink="inkBinding"
    class="sk-btn"
    :class="[`sk-btn--${variant}`, `sk-btn--${size}`]"
    :type="as === 'button' ? type : undefined"
    :href="as === 'a' ? href : undefined"
    :disabled="as === 'button' && disabled ? true : undefined"
    :aria-disabled="as === 'a' && disabled ? 'true' : undefined"
  >
    <slot />
  </component>
</template>
