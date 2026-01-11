<script setup lang="ts">
interface PageLink {
  displayText: string
  url: string
  target: string
}

const { item } = defineProps<{
  item: PageLink
}>()
</script>

<template>
  <a
    v-if="item?.url"
    class="menu-item btn-no-style relative flex h-full items-center overflow-hidden bg-transparent no-underline"
    :href="item.url.stripTilde()"
    :target="item.target"
  >
    <span class="relative z-20 flex h-full w-full items-center px-4">
      {{ item.displayText }}
    </span>
    <span class="background absolute bottom-0 left-0 z-10 h-0 w-full bg-crust transition-all" />
  </a>
</template>

<style scoped>
.menu-item {
  &:focus-visible span {
    outline: 2px solid var(--color-bone);
    outline-offset: -2px;
  }

  &:hover .background,
  &:has([aria-current='page']) .background {
    height: 100%;
  }
}
</style>
