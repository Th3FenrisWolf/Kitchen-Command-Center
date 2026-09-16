<!-- #region ComingSoonBadge Component -->
<script lang="ts">
  /**
   * Badge preset carrying the localized "coming soon" label.
   */
  export default {
    name: 'ComingSoonBadge',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  // Imports stay here: a whitespace-only <script setup> is treated as absent, and the template
  // then loses the bindings for components declared in the block above.
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'
</script>

<template>
  <Badge class="bg-desk-2 text-ink-soft"><ResourceString shared for="ComingSoon" /></Badge>
</template>
