<script setup lang="ts">
const { route } = defineProps<{
  route: { name: string; path: string } | undefined
}>()
</script>

<template>
  <RouterLink
    v-if="route"
    class="menu-item btn-no-style relative flex h-full items-center overflow-hidden bg-transparent no-underline"
    :to="route.path"
  >
    <span class="relative z-20 flex h-full w-full items-center px-4">
      {{ route.name }}
    </span>
    <span class="background absolute bottom-0 left-0 z-10 h-0 w-full bg-crust transition-all" />
  </RouterLink>
</template>

<style scoped>
.menu-item {
  &:focus-visible span {
    outline: 2px solid var(--color-bone);
    outline-offset: -2px;
  }

  &:hover .background,
  &.router-link-active .background {
    height: 100%;
  }
}
</style>
