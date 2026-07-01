<script setup lang="ts">
  defineProps<{
    image?: string
    icon?: string
    accent: string
    imageAlt: string
  }>()
</script>

<template>
  <section class="my-4 rounded-3xl bg-surface-200/50 p-4">
    <div class="flex flex-col gap-6 rounded-2xl bg-surface-500 p-4 text-bone lg:flex-row lg:items-stretch">
      <img
        v-if="image"
        :src="image"
        :alt="imageAlt"
        class="h-44 w-full rounded-2xl object-cover lg:h-auto lg:w-[200px]"
      />
      <div
        v-else
        class="grid h-44 w-full place-items-center rounded-2xl text-onyx lg:h-auto lg:w-[200px]"
        :class="`bg-${accent}`"
        aria-hidden="true"
      >
        <i :class="icon" class="text-7xl"></i>
      </div>

      <div class="flex min-w-0 flex-1 flex-col">
        <slot />
        <div v-if="$slots.action" class="mt-4 lg:hidden">
          <slot name="action" />
        </div>
      </div>
    </div>
  </section>
</template>
