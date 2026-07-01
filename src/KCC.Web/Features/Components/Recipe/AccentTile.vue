<script setup lang="ts">
  import { brandColorFor } from '~/Utilities/BrandColor'

  defineProps<{
    seed: string
    icon?: string
    image?: string
    alt?: string
  }>()
</script>

<template>
  <img v-if="image" :src="image" :alt="alt ?? seed" class="rounded-2xl object-cover" />
  <div v-else class="grid place-items-center rounded-2xl text-onyx" :class="`bg-${brandColorFor(seed)}`" aria-hidden="true">
    <i :class="icon"></i>
  </div>
</template>
