<!-- #region SmallHero Component Properties -->
<script lang="ts">
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  /**
   * Compact page header framing an eyebrow, title, action button, and description.
   */
  export default {
    name: 'SmallHero',
    // The sheet binds `$attrs` itself, so the widget's margin class rides the slip. Inherited as well, every
    // attribute would land there twice.
    inheritAttrs: false,
  }

  export interface SmallHeroProps {
    /**
     * @default false
     */
    dark?: boolean
  }

  export interface SmallHeroSlots {
    eyebrow?: () => void
    title?: () => void
    /**
     * Sits inline with the title, right-aligned.
     */
    'action-button'?: () => void
    description?: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  // `dark` is still accepted so persisted widget configuration keeps deserialising, but it no longer
  // branches the ground: the dual ramp replaced the old two-ground world it was built for.
  defineProps<SmallHeroProps>()

  const { eyebrow, description } = defineSlots<SmallHeroSlots>()
</script>

<template>
  <KccSheet
    as="section"
    :tear="'hero'"
    pad="48px"
    tape
    wash="peach"
    :at="{ x: '100%', y: '100%', w: '34%', h: 'min(60%, 68px)' }"
    v-bind="$attrs"
  >
    <p v-if="eyebrow" class="kcc-kick">
      <slot name="eyebrow"></slot>
    </p>

    <div class="flex flex-wrap items-center justify-between gap-x-7 gap-y-3">
      <h1 class="kcc-h3">
        <slot name="title"></slot>
      </h1>

      <slot name="action-button"></slot>
    </div>

    <p v-if="description" class="kcc-body mt-6">
      <slot name="description"></slot>
    </p>
  </KccSheet>
</template>
