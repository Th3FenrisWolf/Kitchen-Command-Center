<script setup lang="ts">
  import { computed } from 'vue'
  import type { NavRow } from '~/Types/Nav'
  import type { RecipeSearchHit } from '~/Types/Recipe'
  import PadLines from './PadLines.vue'
  import type { NavLabel } from './navLabels'
  import type { RecentlyViewed } from './useRecentlyViewed'

  export interface SearchCardProps {
    query: string
    searched: string
    results: RecipeSearchHit[]
    total: number
    failed: boolean
    recent: RecentlyViewed[]
    suggestions: string[]
    library: string
    recipeTotal: number
    t: NavLabel
  }

  const { query, searched, results, total, failed, recent, suggestions, library, recipeTotal, t } =
    defineProps<SearchCardProps>()

  const emit = defineEmits<{ suggest: [phrase: string] }>()

  const pending = computed(() => query.trim() !== searched)

  const recentLines = computed<NavRow[]>(() =>
    recent.map((page) => ({ label: page.name, url: page.url, icon: 'fa-duotone fa-clock-rotate-left' })),
  )

  const allResults = computed(() => `${library}?${new URLSearchParams({ query: searched })}`)

  const metaOf = (hit: RecipeSearchHit) => [hit.category, ...hit.tags.slice(0, 2)].filter(Boolean).join(' · ')
</script>

<template>
  <div :aria-busy="pending">
    <p v-if="failed" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ t('SearchUnavailable') }}</p>

    <template v-else-if="searched">
      <ul v-if="results.length" class="pad-lines">
        <li v-for="hit in results" :key="hit.slug">
          <a class="pad-result" :href="hit.slug">
            <span class="min-w-0">
              <span class="kcc-h4 pad-result-name">{{ hit.name }}</span>
              <span class="kcc-kick pad-result-meta">{{ metaOf(hit) }}</span>
            </span>
            <span class="kcc-kick kcc-num">{{ hit.fastestTime }}m</span>
          </a>
        </li>
      </ul>
      <template v-else>
        <p class="kcc-body">{{ t('NothingMatches', searched) }}</p>
        <p class="kcc-kick">
          <a class="kcc-link" :href="library">{{ t('AllRecipes', recipeTotal) }}</a>
        </p>
      </template>
      <p v-if="total > results.length" class="kcc-kick">
        <a class="kcc-link" :href="allResults">{{ t('AllResults', total) }}</a>
      </p>
    </template>

    <template v-else>
      <template v-if="recent.length">
        <p class="kcc-kick" data-row style="--i: 0">{{ t('RecentlyViewed') }}</p>
        <PadLines :rows="recentLines" :from="1" glyphs />
      </template>
      <template v-if="suggestions.length">
        <p class="kcc-kick" :class="{ 'mt-6': recent.length }" data-row style="--i: 4">{{ t('Try') }}</p>
        <div class="kcc-badges" data-row style="--i: 5">
          <button
            v-for="phrase in suggestions"
            :key="phrase"
            type="button"
            class="kcc-badge pad-chip"
            @click="emit('suggest', phrase)"
          >
            {{ phrase }}
          </button>
        </div>
      </template>
    </template>
  </div>
</template>

<style>
  .pad-chip {
    color: var(--color-ink);
    cursor: pointer;
    text-decoration: none;
  }

  .pad-chip:hover {
    box-shadow: inset 0 0 0 1px var(--color-ink);
  }

  .pad-result {
    align-items: center;
    color: var(--color-ink);
    display: grid;
    gap: 12px;
    grid-template-columns: minmax(0, 1fr) auto;
    min-height: calc(var(--bl) * 2);
    text-decoration: none;
  }

  .pad-result-name,
  .pad-result-meta {
    display: block;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .pad-result:hover .pad-result-name {
    text-decoration: underline;
    text-underline-offset: 3px;
  }
</style>
