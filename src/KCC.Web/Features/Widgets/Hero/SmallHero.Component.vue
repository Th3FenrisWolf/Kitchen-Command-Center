<!-- #region SmallHero Component Properties -->
<script lang="ts">
  /**
   * Compact page header framing an eyebrow, title, action button, and description.
   */
  export default {
    name: 'SmallHero',
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
  const { dark = false } = defineProps<SmallHeroProps>()
  defineSlots<SmallHeroSlots>()
</script>

<template>
  <section class="my-8 rounded-3xl bg-surface-200/50 p-4">
    <div :class="['content rounded-2xl p-4', dark ? 'bg-surface-500 text-bone' : 'bg-bone text-onyx']">
      <p class="eyebrow">
        <slot name="eyebrow"></slot>
      </p>

      <div class="flex flex-wrap items-center justify-between gap-x-8">
        <h1>
          <slot name="title"></slot>
        </h1>

        <slot name="action-button"></slot>
      </div>

      <p class="description">
        <slot name="description"></slot>
      </p>
    </div>
  </section>
</template>
