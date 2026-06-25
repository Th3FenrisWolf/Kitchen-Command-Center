<script setup lang="ts">
  import Badge from '~/Components/Badge/Badge.vue'

  defineProps<{
    icon?: string
    dotColor?: string
    value?: string | number | null
    unit?: string
    label: string
    comingSoon?: boolean
  }>()
</script>

<template>
  <div class="rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary">
    <span
      v-if="dotColor"
      class="inline-block h-[11px] w-[11px] rounded-full"
      :class="`bg-${dotColor}`"
      aria-hidden="true"
    ></span>
    <i v-else :class="icon" class="text-base text-onyx-light" aria-hidden="true"></i>

    <div v-if="comingSoon" class="mt-1.5">
      <Badge color="muted">{{ value }}</Badge>
    </div>
    <div v-else class="mt-1.5 font-casual text-2xl leading-none lg:text-3xl">
      {{ value ?? '—' }}<span v-if="unit && value != null" class="text-base"> {{ unit }}</span>
    </div>

    <div class="mt-0.5 text-sm text-onyx-light">{{ label }}</div>
  </div>
</template>
