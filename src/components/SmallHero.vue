<script setup lang="ts">
import cx from '~/utilities/cx'

const { dark = false } = defineProps<{ dark?: boolean }>()
</script>

<template>
  <section
    :class="
      cx(
        'no-margin full-width content-grid pb-8 pt-12',
        dark ? 'bg-base text-bone' : 'bg-bone text-onyx',
      )
    "
  >
    <div class="content">
      <p class="eyebrow">
        <slot name="eyebrow"></slot>
      </p>
      <h1>
        <slot name="title"></slot>
      </h1>
      <p class="description">
        <slot name="description"></slot>
      </p>
    </div>
  </section>
</template>
