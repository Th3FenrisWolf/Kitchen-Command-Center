<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'

  defineProps<{
    name: string
    category?: string
    startedByName?: string
    description: string
    image?: string
    icon?: string
    accent: string
    variantCount: number
    fastestMinutes: number | null
    addVariantUrl: string
  }>()
</script>

<template>
  <section class="my-4 rounded-3xl bg-surface-200/50 p-4">
    <div class="flex flex-col gap-6 rounded-2xl bg-surface-500 p-4 text-bone lg:flex-row lg:items-stretch">
      <img
        v-if="image"
        :src="image"
        :alt="name"
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
        <p class="mb-0.5 text-sm uppercase tracking-wide opacity-75">
          <span v-if="category">{{ category }}</span>
          <span v-if="category && startedByName"> · </span>
          <span v-if="startedByName"><ResourceString for="StartedBy" /> {{ startedByName }}</span>
        </p>
        <h1 class="font-casual text-4.5xl leading-tight tracking-[1px]">{{ name }}</h1>
        <p class="mt-3 max-w-[60ch] text-lg opacity-90">{{ description }}</p>

        <dl class="mt-6 flex flex-wrap items-end gap-x-8 gap-y-4 lg:mt-auto lg:pt-4">
          <div>
            <dd class="font-casual text-3xl leading-none">{{ variantCount }}</dd>
            <dt class="mt-1 text-sm opacity-70"><ResourceString for="Variants" /></dt>
          </div>
          <div class="hidden w-px self-stretch bg-bone/20 sm:block"></div>
          <div>
            <dd class="leading-none"><Badge color="muted"><ResourceString for="ComingSoon" /></Badge></dd>
            <dt class="mt-1 text-sm opacity-70"><ResourceString for="AvgRating" /></dt>
          </div>
          <div class="hidden w-px self-stretch bg-bone/20 sm:block"></div>
          <div v-if="fastestMinutes !== null">
            <dd class="font-casual text-3xl leading-none">{{ fastestMinutes }}<span class="text-xl"> min</span></dd>
            <dt class="mt-1 text-sm opacity-70"><ResourceString for="Fastest" /></dt>
          </div>
          <div class="hidden w-px self-stretch bg-bone/20 sm:block"></div>
          <div>
            <dd class="leading-none"><Badge color="muted"><ResourceString for="ComingSoon" /></Badge></dd>
            <dt class="mt-1 text-sm opacity-70"><ResourceString for="CookedIt" /></dt>
          </div>
        </dl>

        <AppLink
          :href="addVariantUrl"
          class="mt-4 block rounded-2xl bg-bone py-3 text-center text-xl font-bold text-onyx lg:hidden"
        >
          ＋ <ResourceString for="AddVariant" />
        </AppLink>
      </div>
    </div>
  </section>
</template>
