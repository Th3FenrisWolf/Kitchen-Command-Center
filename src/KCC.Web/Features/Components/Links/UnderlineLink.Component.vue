<script setup lang="ts">
import cx from '~/Utilities/CX'

const { color, linkTo } = defineProps<{ color: string; linkTo: string }>()
</script>

<template>
  <AppLink :href="linkTo" class="group relative my-1 inline-block focus:outline-0">
    <span>
      <slot />
    </span>
    <span
      :class="
        cx(
          'absolute right-0 bottom-0 left-1/2 h-1 w-0 rounded-full transition-all group-hover:left-0 group-hover:w-full group-focus:left-0 group-focus:w-full',
          color,
        )
      "
    />
  </AppLink>
</template>
