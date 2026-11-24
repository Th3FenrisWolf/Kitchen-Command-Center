<script setup lang="ts">
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import PartFailed from './PartFailed.vue'

  // The submitted sheet's own props arrive here too; none of them may land on the sheet as attributes.
  defineOptions({ inheritAttrs: false })

  defineProps<{ error?: Error }>()
</script>

<template>
  <KccSheet
    wash-key="wizard-submitted"
    margin
    class="mt-rule-1"
    wash="green"
    :tear="2"
    wide
    :aria-busy="error ? undefined : 'true'"
    :data-later-pending="error ? undefined : ''"
  >
    <PartFailed v-if="error" />
  </KccSheet>
</template>
