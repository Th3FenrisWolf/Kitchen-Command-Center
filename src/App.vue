<script setup lang="ts">
import AppHeader from '~/components/AppHeader.vue'
import AppFooter from '~/components/AppFooter.vue'

import '~/utilities/string.extensions'
</script>

<template>
  <AppHeader class="content-grid" />

  <main role="main" class="content-grid bg-bone">
    <RouterView />
  </main>

  <AppFooter class="content-grid" />
</template>

<style>
@import 'tailwindcss';
@import './styles/layout.css';
@import './styles/tailwind-config.css';
@import './styles/typography.css';

section {
  margin-block: 1rem;

  &:first-of-type {
    margin-block-start: 2rem;
  }

  &:last-of-type {
    margin-block-end: 2rem;
  }
}

::-webkit-scrollbar {
  display: none;
}
</style>
