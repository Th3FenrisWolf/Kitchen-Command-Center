<script setup lang="ts">
import cx from '~/utilities/cx'

const { color, linkTo } = defineProps<{ color: string; linkTo: string }>()
</script>

<template>
  <div class="group/fill-up-link relative my-1 inline-block">
    <a :href="linkTo" class="peer focus:outline-0">
      <slot />
    </a>
    <div
      :class="
        cx(
          'absolute bottom-0 left-1/2 right-0 h-1 w-0 rounded-full transition-all group-hover/fill-up-link:left-0 group-hover/fill-up-link:w-full peer-focus:left-0 peer-focus:w-full',
          color,
        )
      "
    />
  </div>
</template>
