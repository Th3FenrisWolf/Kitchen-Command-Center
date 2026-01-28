<script setup lang="ts">
import type { ContentRegions } from '~/Types/ContentRegions'

const { headerContent, bodyContent, footerContent } = defineProps<ContentRegions>()
</script>

<template>
  <component :is="{ name: 'HeaderContent', template: headerContent }" />

  <main role="main" class="content-grid auto-rows-min">
    <component :is="{ name: 'BodyContent', template: bodyContent }" />
  </main>

  <component :is="{ name: 'FooterContent', template: footerContent }" />
</template>
