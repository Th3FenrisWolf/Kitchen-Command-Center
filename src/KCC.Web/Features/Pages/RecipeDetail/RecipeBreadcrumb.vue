<script setup lang="ts">
  import type { Breadcrumb } from '~/Types/Recipe'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  defineProps<{ items: Breadcrumb[] }>()
</script>

<template>
  <nav class="flex min-w-0 flex-wrap items-center gap-2 text-base text-onyx-light" aria-label="Breadcrumb">
    <template v-for="(item, index) in items" :key="index">
      <span v-if="index > 0" class="opacity-50" aria-hidden="true">/</span>
      <AppLink
        v-if="item.url && index < items.length - 1"
        :href="item.url"
        class="text-onyx-light hover:text-onyx"
      >
        {{ item.linkText }}
      </AppLink>
      <span v-else class="font-bold text-onyx">{{ item.linkText }}</span>
    </template>
  </nav>
</template>
