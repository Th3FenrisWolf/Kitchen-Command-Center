<script setup lang="ts">
  const { href } = defineProps<{ href: string }>()
</script>

<template>
  <Link :href="href.stripTilde()" class="group relative my-1 inline-block focus:outline-0">
    <span>
      <slot />
    </span>
    <span
      :class="[
        'absolute right-0 bottom-0 left-1/2 h-1 w-0 rounded-full bg-current transition-all group-hover:left-0 group-hover:w-full group-focus:left-0 group-focus:w-full',
      ]"
    />
  </Link>
</template>
