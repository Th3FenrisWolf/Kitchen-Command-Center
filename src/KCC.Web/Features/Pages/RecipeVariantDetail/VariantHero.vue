<script setup lang="ts">
  import { computed } from 'vue'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { accentForName } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString, useStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'

  const props = defineProps<{
    variantName: string
    recipeName: string
    recipeSlug: string
    images?: ImageItem[]
    icon?: string
    createdByName?: string
  }>()

  const t = useStrings()
  const coverImage = computed(() => props.images?.[0]?.Asset?.Url)
  const accent = computed(() => accentForName(props.variantName))
</script>

<template>
  <section class="my-4 rounded-3xl bg-surface-200/50 p-4">
    <div class="flex flex-col gap-6 rounded-2xl bg-surface-500 p-4 text-bone lg:flex-row lg:items-stretch">
      <img
        v-if="coverImage"
        :src="coverImage"
        :alt="variantName"
        class="h-44 w-full rounded-2xl object-cover lg:h-auto lg:w-[200px]"
      />
      <div
        v-else
        class="grid h-44 w-full place-items-center rounded-2xl text-onyx lg:h-auto lg:w-[200px]"
        :class="`bg-${accent}`"
        aria-hidden="true"
      >
        <i :class="icon || 'fa-solid fa-utensils'" class="text-7xl"></i>
      </div>

      <div class="flex min-w-0 flex-1 flex-col">
        <div class="min-w-0">
          <p class="mb-0.5 text-sm uppercase tracking-wide opacity-75">
            <ResourceString for="VariantOf" />
            <AppLink :href="recipeSlug" class="underline-offset-2 hover:underline">{{ recipeName }}</AppLink>
          </p>
          <h1 class="font-casual text-3xl leading-tight tracking-[1px] lg:text-4.5xl">{{ variantName }}</h1>
          <div class="mt-3 flex flex-wrap items-center gap-4 text-base">
            <span class="inline-flex items-center gap-1.5">
              <i class="fa-solid fa-star text-peach"></i>
              <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
            </span>
            <span class="inline-flex items-center gap-1.5 opacity-85">
              <i class="fa-solid fa-fire-burner"></i>
              <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
            </span>
            <span v-if="createdByName" class="opacity-85"><ResourceString for="By" /> {{ createdByName }}</span>
          </div>
        </div>

        <div class="mt-4 flex flex-wrap items-center gap-3 lg:mt-auto lg:justify-end lg:pt-6">
          <button
            type="button"
            disabled
            :title="t('ComingSoon')"
            class="inline-flex w-full cursor-not-allowed items-center justify-center gap-2 rounded-2xl bg-bone px-6 py-3 font-bold text-onyx opacity-60 lg:w-auto"
          >
            <i class="fa-solid fa-play text-sm"></i> <ResourceString for="CookMode" />
          </button>
          <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
        </div>
      </div>
    </div>
  </section>
</template>
