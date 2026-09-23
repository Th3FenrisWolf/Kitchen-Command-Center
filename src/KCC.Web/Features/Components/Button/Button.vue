<!-- #region Button Component Properties -->
<script lang="ts">
  /**
   * Torn & Waxed pill button. `marker` (default) is the filled primary action; `ghost` is a hairline
   * secondary pill; `ink` is a filled dark pill; `text` is an underlined link, no pill at all. Razor gets
   * the same look from `ButtonLinkTagHelper`, which emits the same `kcc-btn` classes.
   */
  export default {
    // Not `Button`: `resolveDynamicComponent` checks the rendering component's own name first, so
    // `<component :is="'button'">` below would resolve this component to itself and recurse until the
    // stack blows. Any name that is not a capitalized native tag keeps `as` resolving to the element.
    name: 'KccButton',
  }

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
</script>
<!-- #endregion -->

<script setup lang="ts">
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
