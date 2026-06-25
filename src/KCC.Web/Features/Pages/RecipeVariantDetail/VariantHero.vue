<script setup lang="ts">
  import { computed } from 'vue'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { accentForName } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString, useStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import DetailHero from '~/Components/Detail/DetailHero.vue'

  const props = defineProps<{
    variantName: string
    recipeName: string
    recipeSlug: string
    variantDescription: string
    tags: string[]
    images?: ImageItem[]
    icon?: string
    createdByName?: string
  }>()

  const t = useStrings()
  const coverImage = computed(() => props.images?.[0]?.Asset?.Url)
  const accent = computed(() => accentForName(props.variantName))

  // Deterministic accent palette for tag badges (matches the design's varied chips).
  const tagAccents = ['peach', 'yellow', 'maroon', 'sky', 'green', 'lavender'] as const
  const tagColor = (index: number) => tagAccents[index % tagAccents.length]
</script>

<template>
  <DetailHero
    :image="coverImage"
    :icon="icon || 'fa-solid fa-utensils'"
    :accent="accent"
    :image-alt="variantName"
  >
    <p class="mb-0.5 text-sm uppercase tracking-wide opacity-75">
      <ResourceString for="VariantOf" />
      <AppLink :href="recipeSlug" class="underline-offset-2 hover:underline">{{ recipeName }}</AppLink>
    </p>
    <h1 class="font-casual text-3xl leading-tight tracking-[1px] lg:text-4.5xl">{{ variantName }}</h1>

    <div class="mt-3 flex flex-wrap items-center gap-4 text-base">
      <span class="inline-flex items-center gap-1.5">
        <i class="fa-solid fa-star text-peach" aria-hidden="true"></i>
        <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      </span>
      <span class="inline-flex items-center gap-1.5 opacity-85">
        <i class="fa-solid fa-fire-burner" aria-hidden="true"></i>
        <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      </span>
      <span v-if="createdByName" class="opacity-85"><ResourceString for="By" /> {{ createdByName }}</span>
    </div>

    <p class="mt-3 max-w-[60ch] text-lg opacity-90">{{ variantDescription }}</p>

    <div v-if="tags.length" class="mt-4 flex flex-wrap gap-2">
      <Badge v-for="(tag, i) in tags" :key="tag" :color="tagColor(i)">{{ tag }}</Badge>
    </div>

    <template #action>
      <button
        type="button"
        disabled
        :title="t('ComingSoon')"
        class="flex w-full cursor-not-allowed items-center justify-center gap-2 rounded-2xl bg-bone py-3 font-bold text-onyx opacity-60"
      >
        <i class="fa-solid fa-play text-sm" aria-hidden="true"></i> <ResourceString for="CookMode" />
      </button>
    </template>
  </DetailHero>
</template>
