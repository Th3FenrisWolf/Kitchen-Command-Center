<!-- #region AccountView Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  /**
   * Member's profile page: identity card, contribution counts, and their recipes and variants.
   */
  export default {
    name: 'AccountView',
  }

  interface ProfileVariant {
    pageId: number
    name: string
    icon?: string
    url?: string
    isPending: boolean
  }

  interface RecipeGroup {
    pageId: number
    recipeName: string
    recipeIcon?: string
    recipeUrl?: string
    isPending: boolean
    startedByYou: boolean
    variants: ProfileVariant[]
  }

  export interface AccountViewProps {
    displayName: string
    /**
     * Seeds the avatar's monogram and its color.
     */
    initials: string
    memberSince: string
    settingsUrl: string
    logoutUrl: string
    /**
     * Every recipe the member has touched, whether they started it or only added a variant.
     */
    recipeGroups: RecipeGroup[]
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<AccountViewProps>()

  provideResourceStrings(props.resourceStrings, 'Account')

  // Deterministic avatar color (no Math.random — stable across SSR + client hydration).
  const palette = ['bg-peach', 'bg-green', 'bg-teal', 'bg-sky', 'bg-yellow', 'bg-maroon', 'bg-lavender']
  const avatarColor = computed(() => {
    const seed = [...props.initials].reduce((sum, c) => sum + c.charCodeAt(0), 0)
    return palette[seed % palette.length]
  })

  const recipesStartedCount = computed(() => props.recipeGroups.filter((group) => group.startedByYou).length)
  const variantsCount = computed(() => props.recipeGroups.reduce((sum, group) => sum + group.variants.length, 0))

  const comingSoonSections = [
    { key: 'Favorites', dot: 'bg-maroon' },
    { key: 'RecentActivity', dot: 'bg-teal' },
  ]
</script>

<template>
  <section class="grid w-full gap-8 py-10 md:grid-cols-content-aside">
    <!-- Identity card -->
    <aside class="h-max rounded-3xl bg-paper-2 p-8 text-center md:sticky md:top-10">
      <div :class="['mx-auto mb-4 grid h-24 w-24 place-items-center rounded-full text-3xl font-bold text-ink', avatarColor]">
        {{ initials }}
      </div>
      <h1 class="text-2xl">{{ displayName }}</h1>
      <p class="mt-1 text-ink-soft"><ResourceString for="MemberSince" /> {{ memberSince }}</p>

      <div class="mt-6 grid gap-3">
        <a :href="settingsUrl" class="rounded-2xl bg-paper px-4 py-2 text-ink">
          <ResourceString for="AccountSettings" />
        </a>
        <a :href="logoutUrl" class="rounded-2xl border border-ink px-4 py-2 text-ink">
          <ResourceString for="SignOut" />
        </a>
      </div>
    </aside>

    <!-- Content column -->
    <div class="grid gap-5">
      <!-- My Recipes & Variants -->
      <article class="rounded-3xl bg-paper-2 p-6">
        <header class="mb-4 flex items-center justify-between">
          <h2 class="flex items-center gap-2 text-xl">
            <span class="inline-block h-2.5 w-2.5 rounded-full bg-peach" />
            <ResourceString for="MyRecipesAndVariants" />
          </h2>
          <span v-if="recipeGroups.length" class="text-sm text-ink-soft">
            {{ recipesStartedCount }} <ResourceString for="RecipesLabel" /> · {{ variantsCount }}
            <ResourceString for="VariantsLabel" />
          </span>
        </header>

        <p v-if="!recipeGroups.length" class="text-ink-soft"><ResourceString for="NoCreationsYet" /></p>

        <div v-else class="grid gap-4">
          <div v-for="group in recipeGroups" :key="group.pageId" class="rounded-2xl bg-desk-2 p-4">
            <div class="flex flex-wrap items-center gap-2">
              <i v-if="group.recipeIcon" :class="group.recipeIcon" class="text-xl"></i>
              <component
                :is="group.recipeUrl ? AppLink : 'span'"
                :href="group.recipeUrl || undefined"
                class="font-casual text-2xl"
              >
                {{ group.recipeName }}
              </component>
              <span v-if="group.startedByYou" class="rounded-full bg-teal px-2 py-0.5 text-xs font-bold text-ink-on-wash">
                <ResourceString for="StartedByYou" />
              </span>
              <span v-if="group.isPending" class="rounded-full bg-yellow px-2 py-0.5 text-xs font-bold text-ink-on-wash">
                <ResourceString for="PendingReview" />
              </span>
            </div>
            <ul v-if="group.variants.length" class="mt-2 grid gap-1 pl-7">
              <li v-for="variant in group.variants" :key="variant.pageId" class="flex flex-wrap items-center gap-2">
                <i v-if="variant.icon" :class="variant.icon"></i>
                <component :is="variant.url ? AppLink : 'span'" :href="variant.url || undefined">
                  {{ variant.name }}
                </component>
                <span v-if="variant.isPending" class="rounded-full bg-yellow px-2 py-0.5 text-xs font-bold text-ink-on-wash">
                  <ResourceString for="PendingReview" />
                </span>
              </li>
            </ul>
          </div>
        </div>
      </article>

      <!-- Remaining "Coming soon" kitchen sections -->
      <article v-for="section in comingSoonSections" :key="section.key" class="rounded-3xl bg-paper-2 p-6">
        <header class="mb-4 flex items-center justify-between">
          <h2 class="flex items-center gap-2 text-xl">
            <span :class="['inline-block h-2.5 w-2.5 rounded-full', section.dot]" />
            <ResourceString :for="section.key" />
          </h2>
          <span class="rounded-full bg-yellow px-3 py-1 text-xs font-bold text-ink-on-wash">
            <ResourceString for="ComingSoon" />
          </span>
        </header>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-4 md:grid-cols-2 lg:grid-cols-4">
          <div v-for="n in 4" :key="n" class="h-20 rounded-xl bg-desk-2 opacity-70" />
        </div>
      </article>
    </div>
  </section>
</template>
