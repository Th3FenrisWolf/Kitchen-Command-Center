<!-- #region App Component -->
<script lang="ts">
  import type { ContentRegions } from '~/Types/ContentRegions'

  /**
   * Root shell that compiles each server-rendered content region into its own child component.
   */
  export default {
    name: 'App',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { headerContent, bodyContent, footerContent } = defineProps<ContentRegions>()
</script>

<template>
  <span class="kcc-grain" aria-hidden="true"></span>

  <component :is="{ name: 'HeaderContent', template: headerContent }" />

  <main role="main" class="content-grid auto-rows-min">
    <component :is="{ name: 'BodyContent', template: bodyContent }" />
  </main>

  <component :is="{ name: 'FooterContent', template: footerContent }" />

  <span class="kcc-crayon" aria-hidden="true"></span>
</template>
