<script setup lang="ts">
  import type { Breadcrumb } from '~/Types/Recipe'
  import { computed } from 'vue'

  export interface BreadcrumbProps {
    items: Breadcrumb[]
  }

  const { items } = defineProps<BreadcrumbProps>()
  const home = computed(() => items[0]!)
  const ancestors = computed(() => items.slice(1))
</script>

<template>
  <nav aria-label="Breadcrumb">
    <ol class="flex min-w-0 flex-wrap items-center gap-2" role="list">
      <li class="kcc-kick">
        <a :href="home.url" class="kcc-link kcc-link--icon" :aria-label="home.linkText">
          <i class="fa-duotone fa-house" aria-hidden="true"></i>
        </a>
      </li>

      <template v-for="(item, index) in ancestors" :key="index">
        <li class="kcc-kick" aria-hidden="true">·</li>
        <li v-if="index === ancestors.length - 1" class="kcc-kick text-ink" aria-current="page">{{ item.linkText }}</li>
        <li v-else-if="item.url">
          <a :href="item.url" class="kcc-kick kcc-link">{{ item.linkText }}</a>
        </li>
        <li v-else class="kcc-kick">{{ item.linkText }}</li>
      </template>
    </ol>
  </nav>
</template>
