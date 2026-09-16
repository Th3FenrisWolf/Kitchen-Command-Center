<!-- #region VariantCookedToggle Component Properties -->
<script lang="ts">
  import { ref } from 'vue'
  import { post, del } from '~/Utilities/Api'
  import { ResourceString } from '~/Components/ResourceStrings'

  /**
   * Button recording that the member cooked a variant, showing the running tally.
   */
  export default {
    name: 'VariantCookedToggle',
  }

  export interface VariantCookedToggleProps {
    variantGuid: string
    /**
     * Server-rendered starting values; the API's response drives them from the first click on.
     * @default 0
     */
    cookedCount?: number
    /**
     * @default false
     */
    hasCooked?: boolean
    /**
     * Hides the button entirely when false.
     * @default false
     */
    isAuthenticated?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
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
  <button
    v-if="isAuthenticated"
    type="button"
    data-testid="cooked-toggle"
    class="flex-none rounded-2xl px-4 py-2 font-bold transition-colors"
    :class="cooked ? 'bg-maroon text-ink-on-wash' : 'bg-paper-2 text-ink-on-wash'"
    :disabled="busy"
    @click="toggleCooked"
  >
    <i :class="cooked ? 'fa-solid fa-fire-burner' : 'fa-regular fa-fire-burner'" aria-hidden="true"></i>
    <ResourceString for="ICookedThis" /> ({{ cookedCount }})
  </button>
</template>
