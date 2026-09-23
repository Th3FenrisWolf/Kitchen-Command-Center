<!-- #region Breadcrumb Component Properties -->
<script lang="ts">
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import type { Breadcrumb } from '~/Types/Recipe'

  /**
   * Dot-separated trail of ancestor links, starting from a home icon.
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
  import { computed } from 'vue'
  const { items } = defineProps<BreadcrumbProps>()
  const home = computed(() => items[0]!)
  const ancestors = computed(() => items.slice(1))
</script>

<template>
  <nav aria-label="Breadcrumb">
    <ol class="flex min-w-0 flex-wrap items-center gap-2" role="list">
      <li class="kcc-kick">
        <AppLink :href="home.url" class="kcc-link kcc-link--icon" :aria-label="home.linkText">
          <i class="fa-duotone fa-house" aria-hidden="true"></i>
        </AppLink>
      </li>

      <template v-for="(item, index) in ancestors" :key="index">
        <li class="kcc-kick" aria-hidden="true">·</li>
        <li v-if="index === ancestors.length - 1" class="kcc-kick text-ink" aria-current="page">{{ item.linkText }}</li>
        <li v-else-if="item.url">
          <AppLink :href="item.url" class="kcc-kick kcc-link">{{ item.linkText }}</AppLink>
        </li>
        <li v-else class="kcc-kick">{{ item.linkText }}</li>
      </template>
    </ol>
  </nav>
</template>
