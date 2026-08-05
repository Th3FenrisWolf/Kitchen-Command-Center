<!-- #region Breadcrumb Component Properties -->
<script lang="ts">
  import type { Breadcrumb } from '~/Types/Recipe'

  export interface BreadcrumbProps {
    /**
     * The list of breadcrumbs to display
     */
    items: Breadcrumb[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  const { items } = defineProps<BreadcrumbProps>()
</script>

<template>
  <nav class="flex min-w-0 flex-wrap items-center gap-2 text-base text-onyx-light" aria-label="Breadcrumb">
    <AppLink :href="items[0].url" class="text-onyx-light transition-colors hover:text-onyx" :aria-label="items[0].linkText">
      <i class="fa-duotone fa-house" />
    </AppLink>
    <span class="opacity-50" aria-hidden="true">/</span>

    <template v-for="(item, index) in items.slice(1)" :key="index">
      <span v-if="index > 0" class="opacity-50" aria-hidden="true">/</span>
      <AppLink
        v-if="item.url && index < items.slice(1).length - 1"
        :href="item.url"
        class="text-onyx-light transition-colors hover:text-onyx"
      >
        {{ item.linkText }}
      </AppLink>
      <span v-else class="font-bold text-onyx">{{ item.linkText }}</span>
    </template>
  </nav>
</template>
