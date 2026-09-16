<!-- #region Sheet Component Properties -->
<script lang="ts">
  import type { InkOptions } from '~/Ink/inkDom'
  import type { BrandBackgroundColor } from '~/Types/DesignSystem'

  /**
   * A sheet of stock on the desk: paper ground, drawn outline, hatched shadow, optional wash, marker tab,
   * dog-ear and ruling. Every panel in the app is one of these.
   */
  export default {
    name: 'Sheet',
  }

  /** A wash hue — the brand `bg-*` tokens without their prefix. */
  export type Wash = BrandBackgroundColor extends `bg-${infer Hue}` ? Hue : never

  export type Ruling = boolean | 'lines' | 'dots' | 'grid' | 'margin'

  export interface SheetProps {
    /** Pools a wash under the content. */
    wash?: Wash

    /** Where the wash sits, as CSS lengths or percentages. */
    washAt?: { x?: string; y?: string; w?: string; h?: string }

    /** Marker tab clipped to the top edge. */
    tab?: string

    /** Font Awesome classes for the tab's icon. */
    tabIcon?: string

    /**
     * Dog-ear on the top-right corner.
     * @default true
     */
    fold?: boolean

    /**
     * Rule pattern; `true` means lines.
     * @default false
     */
    ruled?: Ruling

    /** Overrides `--rule-pitch` for this sheet, e.g. `'24px'`. */
    rulePitch?: string

    /** Overrides `--rule-offset` for this sheet. */
    ruleOffset?: string

    /**
     * Hatched shadow under the sheet.
     * @default true
     */
    hatch?: boolean

    /**
     * Drawn outline; pass options to tune it or `false` to omit it.
     * @default true
     */
    ink?: boolean | InkOptions

    /**
     * @default 'md'
     */
    pad?: 'sm' | 'md' | 'lg'

    /**
     * @default 'div'
     */
    as?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { computed } from 'vue'

  const {
    wash,
    washAt = {},
    tab,
    tabIcon,
    fold = true,
    ruled = false,
    rulePitch,
    ruleOffset,
    hatch = true,
    ink = true,
    pad = 'md',
    as = 'div',
  } = defineProps<SheetProps>()

  const ruledClasses = computed(() => {
    if (ruled === false) return []
    const pattern = ruled === true ? 'lines' : ruled
    return pattern === 'lines' ? ['sk-ruled'] : ['sk-ruled', `sk-ruled--${pattern}`]
  })

  const sheetStyle = computed(() => ({
    ...(rulePitch ? { '--rule-pitch': rulePitch } : {}),
    ...(ruleOffset ? { '--rule-offset': ruleOffset } : {}),
  }))

  const washStyle = computed(() =>
    wash ? { '--c': `var(--color-${wash})`, '--x': washAt.x, '--y': washAt.y, '--w': washAt.w, '--h': washAt.h } : undefined,
  )

  const inkBinding = computed(() => (ink === false ? null : { kind: 'sheet' as const, hatch, ...(ink === true ? {} : ink) }))
</script>

<template>
  <component
    :is="as"
    v-ink="inkBinding"
    class="sk-sheet"
    :class="[`sk-sheet--${pad}`, fold && 'sk-fold', tab && 'sk-tabbed', ...ruledClasses]"
    :style="sheetStyle"
  >
    <span v-if="tab" class="sk-tab"><i v-if="tabIcon" :class="tabIcon" aria-hidden="true"></i>{{ tab }}</span>
    <span v-if="wash" class="sk-wash" :style="washStyle" aria-hidden="true"></span>
    <slot />
  </component>
</template>
