<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import DetailHero from '~/Components/Detail/DetailHero.vue'

  defineProps<{
    name: string
    category?: string
    startedByName?: string
    description: string
    image?: string
    icon?: string
    accent: string
    addVariantUrl: string
  }>()
</script>

<template>
  <DetailHero :image="image" :icon="icon" :accent="accent" :image-alt="name">
    <p class="mb-0.5 text-sm uppercase tracking-wide opacity-75">
      <span v-if="category">{{ category }}</span>
      <span v-if="category && startedByName"> · </span>
      <span v-if="startedByName"><ResourceString for="StartedBy" /> {{ startedByName }}</span>
    </p>
    <h1 class="font-casual text-4.5xl leading-tight tracking-[1px]">{{ name }}</h1>

    <div class="mt-3 flex flex-wrap items-center gap-4 text-base">
      <span class="inline-flex items-center gap-1.5">
        <i class="fa-solid fa-star text-peach" aria-hidden="true"></i>
        <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      </span>
      <span class="inline-flex items-center gap-1.5 opacity-85">
        <i class="fa-solid fa-fire-burner" aria-hidden="true"></i>
        <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      </span>
    </div>

    <p class="mt-3 max-w-[60ch] text-lg opacity-90">{{ description }}</p>

    <template #action>
      <AppLink
        :href="addVariantUrl"
        class="block rounded-2xl bg-bone py-3 text-center text-xl font-bold text-onyx"
      >
        ＋ <ResourceString for="AddVariant" />
      </AppLink>
    </template>
  </DetailHero>
</template>
