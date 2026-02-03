<script setup lang="ts">
import cx from '~/Utilities/CX'

const { dark = false } = defineProps<{ dark?: boolean }>()
</script>

<template>
  <section
    :class="
      cx(
        'no-margin full-width content-grid pt-12 pb-8',
        dark ? 'bg-base text-bone' : 'bg-bone text-onyx',
      )
    "
  >
    <div class="content">
      <p class="eyebrow">
        <slot name="eyebrow"></slot>
      </p>
      <div class="flex items-center justify-between">
        <h1>
          <slot name="title"></slot>
        </h1>

        <slot name="actionButton"></slot>
      </div>
      <p class="description">
        <slot name="description"></slot>
      </p>
    </div>
  </section>
</template>
