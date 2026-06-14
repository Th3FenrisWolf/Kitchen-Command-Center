<script setup lang="ts">
  import { cx } from '~/Utilities/CX'

  const { dark = false } = defineProps<{ dark?: boolean }>()
</script>

<template>
  <section class="my-8 rounded-3xl bg-surface-200/50 p-4">
    <div :class="cx('content rounded-2xl p-4', dark ? 'bg-surface-500 text-bone' : 'bg-bone text-onyx')">
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
