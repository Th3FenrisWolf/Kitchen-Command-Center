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
  // `dark` is still accepted so persisted widget configuration keeps deserialising, but it no longer
  // branches the ground: the dual ramp replaced the old two-ground world it was built for.
  defineProps<SmallHeroProps>()
  defineSlots<SmallHeroSlots>()
</script>

<template>
  <section v-ink="'sheet'" class="sk-sheet sk-fold my-8">
    <span class="sk-wash" style="--c: var(--color-peach)" aria-hidden="true"></span>
    <div class="content rounded-2xl p-4 text-ink">
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
