<script setup lang="ts">
  import { ref } from 'vue'
  import { post, del } from '~/Utilities/Api'
  import { ResourceString } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'

  export interface VariantCookedToggleProps {
    variantGuid: string
    cookedCount?: number
    hasCooked?: boolean
    isAuthenticated?: boolean
  }

  const {
    variantGuid,
    cookedCount: initialCookedCount = 0,
    hasCooked = false,
    isAuthenticated = false,
  } = defineProps<VariantCookedToggleProps>()

  const cooked = ref(hasCooked)
  const cookedCount = ref(initialCookedCount)
  const busy = ref(false)

  const toggleCooked = async () => {
    if (busy.value) return
    busy.value = true
    const result = cooked.value
      ? await del<{ cookedCount: number; hasCooked: boolean }>(`/api/variant/${variantGuid}/cooked`)
      : await post<{ cookedCount: number; hasCooked: boolean }>(`/api/variant/${variantGuid}/cooked`)
    busy.value = false
    if (result.success) {
      cooked.value = result.data.hasCooked
      cookedCount.value = result.data.cookedCount
    }
  }
</script>

<template>
  <Button
    v-if="isAuthenticated"
    :variant="cooked ? 'ink' : 'ghost'"
    data-testid="cooked-toggle"
    :aria-pressed="cooked"
    :disabled="busy"
    @click="toggleCooked"
  >
    <i class="fa-duotone fa-fire-burner" aria-hidden="true"></i>
    <!-- The e2e reads the tally out of the button's own text, so the brackets stay inside one flex item. -->
    <span
      ><ResourceString for="ICookedThis" /> (<span class="kcc-num">{{ cookedCount }}</span
      >)</span
    >
  </Button>
</template>
