<!-- #region KccSheet Component Properties -->
<script lang="ts">
  import type { Wash } from '~/Types/DesignSystem'

  /**
   * A sheet torn off the pad: tilt → torn (fibre + fall) → sheet (clip + rule + wash), with the printed label
   * and the tape outside the tear. Every panel in the app is one of these. Renders entirely on the server.
   */
  export default {
    name: 'KccSheet',
  }

  export type Tear = 1 | 2 | 3 | 4 | 5 | 6 | 'hero'

  export interface KccSheetProps {
    /** Marker pill over the top-left edge. */
    label?: string

    /** Font Awesome classes for the label's icon. */
    icon?: string

    /** Moves the label to the right edge. */
    labelRight?: boolean

    /** Pools a wash under the content. */
    wash?: Wash

    /** Where the wash sits, as percentages of the sheet. */
    at?: { x?: string; y?: string; w?: string; h?: string }

    /**
     * Which tear preset clips the sheet. Neighbours must differ: pass `(index % 6) + 1` in a list.
     * @default 1
     */
    tear?: Tear

    /** One strip of tape, top-centre. */
    tape?: boolean

    /** Sheet padding, e.g. `'48px'`; also aligns the pencil rule. */
    pad?: string

    /** No tilt. For surfaces read closely or from across the kitchen: forms, cook mode. */
    crisp?: boolean

    /**
     * @default 'div'
     */
    as?: 'div' | 'section' | 'article' | 'li'
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { computed } from 'vue'

  const {
    label,
    icon,
    labelRight = false,
    wash,
    at = {},
    tear = 1,
    tape = false,
    pad,
    crisp = false,
    as = 'div',
  } = defineProps<KccSheetProps>()

  const washStyle = computed(() =>
    wash ? { '--c': `var(--color-${wash})`, '--x': at.x, '--y': at.y, '--w': at.w, '--h': at.h } : undefined,
  )

  // Spread via v-bind rather than :style directly: SSR always prints a `style=""` attribute whenever the
  // template has a literal `:style` binding, even when the bound value is `undefined`. An object that omits
  // the `style` key entirely when there is nothing to set keeps the sheet's default markup free of it.
  const sheetAttrs = computed(() => (pad ? { style: { '--pad': pad } } : {}))

  // Inline so it beats the preset class's --r.
  const slipAttrs = computed(() => (crisp ? { style: { '--r': 0 } } : {}))
</script>

<template>
  <component :is="as" class="kcc-slip" :class="`kcc-tear-${tear}`" v-bind="slipAttrs">
    <div class="kcc-torn">
      <div class="kcc-sheet" v-bind="sheetAttrs">
        <span v-if="wash" class="kcc-wash" :style="washStyle" aria-hidden="true"></span>
        <slot />
      </div>
    </div>
    <span v-if="label" class="kcc-label" :class="{ 'kcc-label--right': labelRight }">
      <i v-if="icon" :class="icon" aria-hidden="true"></i>{{ label }}
    </span>
    <span v-if="tape" class="kcc-tape" aria-hidden="true"></span>
  </component>
</template>
