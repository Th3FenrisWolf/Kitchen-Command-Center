<script setup lang="ts">
  import type { Wash } from '~/Types/DesignSystem'
  import { computed } from 'vue'

  export type Tear = 1 | 2 | 3 | 4 | 5 | 6 | 'hero'

  export interface KccSheetProps {
    label?: string
    icon?: string
    labelRight?: boolean
    wash?: Wash
    /**
     * Where the wash sits, as percentages of the sheet. Author-supplied literals written straight into
     * custom properties; never bind CMS or user data here.
     */
    at?: { x?: string; y?: string; w?: string; h?: string }
    /** Neighbours must differ: pass `(index % 6) + 1` in a list. */
    tear?: Tear
    piled?: boolean
    wide?: boolean
    flip?: boolean
    pad?: string
    crisp?: boolean
    margin?: boolean
    blank?: boolean
    scroll?: boolean
    as?: 'div' | 'section' | 'article' | 'li'
  }

  const {
    label,
    icon,
    labelRight = false,
    wash,
    at = {},
    tear = 1,
    piled = true,
    wide = false,
    flip = false,
    pad,
    crisp = false,
    margin = false,
    blank = false,
    scroll = false,
    as = 'div',
  } = defineProps<KccSheetProps>()

  const washStyle = computed(() =>
    wash ? { '--c': `var(--color-${wash})`, '--x': at?.x, '--y': at?.y, '--w': at?.w, '--h': at?.h } : undefined,
  )

  // Spread via v-bind rather than :style directly: SSR always prints a `style=""` attribute whenever the
  // template has a literal `:style` binding, even when the bound value is `undefined`. An object that omits
  // the `style` key entirely when there is nothing to set keeps the sheet's default markup free of it.
  const sheetAttrs = computed(() => (pad ? { style: { '--pad': pad } } : {}))

  // Inline so it beats the preset class's --r.
  const slipAttrs = computed(() => (crisp ? { style: { '--r': 0 } } : {}))
</script>

<template>
  <component
    :is="as"
    class="kcc-slip"
    :class="[{ 'kcc-slip--wide': tear === 'hero' || wide, 'kcc-slip--flip': flip }, `kcc-tear-${tear}`]"
    v-bind="slipAttrs"
  >
    <div class="kcc-torn">
      <div
        class="kcc-sheet"
        :class="{ 'kcc-sheet--margin': margin, 'kcc-sheet--blank': blank, 'kcc-sheet--scroll': scroll }"
        v-bind="sheetAttrs"
      >
        <span v-if="wash" class="kcc-wash" :style="washStyle" aria-hidden="true"></span>
        <slot />
      </div>
    </div>
    <span v-if="label || $slots.label" class="kcc-label" :class="{ 'kcc-label--right': labelRight }">
      <i v-if="icon" :class="icon" aria-hidden="true"></i><slot name="label">{{ label }}</slot>
    </span>
    <span v-if="piled" class="kcc-blank" aria-hidden="true"><span></span></span>
  </component>
</template>
