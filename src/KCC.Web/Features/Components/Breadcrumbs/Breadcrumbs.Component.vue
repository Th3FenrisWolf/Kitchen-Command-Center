<!-- #region Breadcrumb Component Properties -->
<script lang="ts">
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import type { Breadcrumb } from '~/Types/Recipe'

  /**
   * Slash-separated trail of ancestor links, starting from a home icon.
   */
  export default {
    name: 'Breadcrumbs',
  }

  export interface BreadcrumbProps {
    /**
     * Ordered root-first; the first entry becomes the home icon and the last renders unlinked.
     */
    items: Breadcrumb[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { items } = defineProps<BreadcrumbProps>()
</script>

<template>
  <nav aria-label="Breadcrumb">
    <ol class="flex min-w-0 flex-wrap items-center gap-2">
      <li>
        <AppLink :href="items[0]!.url" class="kcc-link" :aria-label="items[0]!.linkText">
          <i class="fa-duotone fa-house" aria-hidden="true"></i>
        </AppLink>
      </li>

      <template v-for="(item, index) in items.slice(1)" :key="index">
        <li class="kcc-kick text-ink-soft" aria-hidden="true">·</li>
        <li v-if="item.url && index < items.slice(1).length - 1">
          <AppLink :href="item.url" class="kcc-kick kcc-link">{{ item.linkText }}</AppLink>
        </li>
        <li v-else class="kcc-kick text-ink" aria-current="page">{{ item.linkText }}</li>
      </template>
    </ol>
  </nav>
</template>
