<script setup lang="ts">
import AppHeader from '~/Components/Header/AppHeader.vue'
import Footer from '~/Components/Footer/Footer.vue'

defineProps<{ serverContent: string }>()
</script>

<template>
  <AppHeader />

  <main role="main" class="content-grid auto-rows-min bg-bone">
    <component :is="{ template: serverContent }" />
  </main>

  <Footer />
</template>
