<script setup lang="ts">
  import { ref } from 'vue'
  import { post, del } from '~/Utilities/Api'
  import { ResourceString } from '~/Components/ResourceStrings'

  const props = withDefaults(
    defineProps<{
      variantGuid: string
      cookedCount?: number
      hasCooked?: boolean
      isAuthenticated?: boolean
    }>(),
    { cookedCount: 0, hasCooked: false, isAuthenticated: false },
  )

  const cooked = ref(props.hasCooked ?? false)
  const cookedCount = ref(props.cookedCount ?? 0)
  const busy = ref(false)

  const toggleCooked = async () => {
    if (busy.value) return
    busy.value = true
    const result = cooked.value
      ? await del<{ cookedCount: number; hasCooked: boolean }>(`/api/variant/${props.variantGuid}/cooked`)
      : await post<{ cookedCount: number; hasCooked: boolean }>(`/api/variant/${props.variantGuid}/cooked`)
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
    :class="cooked ? 'bg-maroon text-bone' : 'bg-bone text-onyx'"
    :disabled="busy"
    @click="toggleCooked"
  >
    <i :class="cooked ? 'fa-solid fa-fire-burner' : 'fa-regular fa-fire-burner'" aria-hidden="true"></i>
    <ResourceString for="ICookedThis" /> ({{ cookedCount }})
  </button>
</template>
