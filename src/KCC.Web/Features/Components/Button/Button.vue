<script setup lang="ts">
  export type ButtonVariant = 'marker' | 'ghost' | 'ink' | 'text'
  export type ButtonSize = 'md' | 'lg'

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

  const {
    variant = 'marker',
    size = 'md',
    as = 'button',
    type = 'button',
    href,
    disabled = false,
  } = defineProps<ButtonProps>()
</script>

<template>
  <component
    :is="as"
    class="kcc-btn"
    :class="[variant !== 'marker' && `kcc-btn--${variant}`, size === 'lg' && 'kcc-btn--lg']"
    :type="as === 'button' ? type : undefined"
    :href="as === 'a' ? href : undefined"
    :disabled="as === 'button' && disabled ? true : undefined"
    :aria-disabled="as === 'a' && disabled ? 'true' : undefined"
  >
    <slot />
  </component>
</template>
