<!-- #region AccountView Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  /**
   * Member's profile page: identity sheet, contribution counts, and their recipes and variants.
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
  import Badge from '~/Components/Badge/Badge.vue'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  const props = defineProps<AccountViewProps>()

  provideResourceStrings(props.resourceStrings, 'Account')

  const recipesStartedCount = computed(() => props.recipeGroups.filter((group) => group.startedByYou).length)
  const variantsCount = computed(() => props.recipeGroups.reduce((sum, group) => sum + group.variants.length, 0))

  // The profile sheet beside the list takes the first tear, so the list starts at the second and no two
  // neighbours are torn alike.
  const tearFor = (index: number) => (((index + 1) % 6) + 1) as 1 | 2 | 3 | 4 | 5 | 6

  const comingSoonSections = [
    { key: 'Favorites', tear: 3 },
    { key: 'RecentActivity', tear: 5 },
  ] as const
</script>

<template>
  <div class="mt-6 grid items-start gap-x-7 gap-y-9 md:grid-cols-content-aside">
    <KccSheet
      as="section"
      wash="lavender"
      :at="{ x: '88%', y: '18%', w: '38%', h: '60%' }"
      :tear="1"
      class="md:col-start-1 md:row-start-1"
    >
      <h1 class="kcc-h3">{{ displayName }}</h1>
      <p class="kcc-kick">
        <ResourceString for="MemberSince" /> <span class="kcc-num">{{ memberSince }}</span>
      </p>

      <div class="kcc-stats mt-6">
        <div>
          <ResourceString for="RecipesLabel" as="p" class="kcc-lbl" />
          <p class="kcc-v">{{ recipesStartedCount }}</p>
        </div>
        <div>
          <ResourceString for="VariantsLabel" as="p" class="kcc-lbl" />
          <p class="kcc-v">{{ variantsCount }}</p>
        </div>
      </div>

      <div class="mt-6 flex flex-wrap items-center gap-3">
        <Button as="a" :href="settingsUrl" variant="ghost">
          <ResourceString for="AccountSettings" />
        </Button>
        <Button as="a" :href="logoutUrl" variant="text">
          <ResourceString for="SignOut" />
        </Button>
      </div>
    </KccSheet>

    <section class="md:col-start-2 md:row-span-2 md:row-start-1">
      <div class="kcc-secname"><ResourceString for="MyRecipesAndVariants" as="h2" /></div>

      <ResourceString v-if="!recipeGroups.length" for="NoCreationsYet" as="p" class="kcc-body" />

      <ul v-else class="grid gap-9">
        <KccSheet v-for="(group, index) in recipeGroups" :key="group.pageId" as="li" :tear="tearFor(index)" pad="16px">
          <div class="flex flex-wrap items-center gap-x-3 gap-y-2">
            <i v-if="group.recipeIcon" :class="group.recipeIcon" aria-hidden="true"></i>
            <h3 class="kcc-h4">
              <component
                :is="group.recipeUrl ? AppLink : 'span'"
                :href="group.recipeUrl || undefined"
                :class="group.recipeUrl && 'kcc-link'"
              >
                {{ group.recipeName }}
              </component>
            </h3>
            <Badge v-if="group.startedByYou"><ResourceString for="StartedByYou" /></Badge>
            <Badge v-if="group.isPending"><ResourceString for="PendingReview" /></Badge>
          </div>

          <ul v-if="group.variants.length" class="mt-6 grid gap-2">
            <li v-for="variant in group.variants" :key="variant.pageId" class="grid grid-cols-[1fr_auto] items-center gap-3">
              <span class="flex flex-wrap items-center gap-2">
                <i v-if="variant.icon" :class="variant.icon" aria-hidden="true"></i>
                <component
                  :is="variant.url ? AppLink : 'span'"
                  :href="variant.url || undefined"
                  :class="variant.url && 'kcc-link'"
                >
                  {{ variant.name }}
                </component>
              </span>
              <Badge v-if="variant.isPending"><ResourceString for="PendingReview" /></Badge>
            </li>
          </ul>
        </KccSheet>
      </ul>
    </section>

    <!-- Kitchen sections the app has not built yet; the lavender sheet says so in the kit's own words. -->
    <div class="grid gap-y-9 md:col-start-1 md:row-start-2">
      <KccSheet
        v-for="section in comingSoonSections"
        :key="section.key"
        as="section"
        wash="lavender"
        :at="{ x: '85%', y: '15%', w: '50%', h: '55%' }"
        icon="fa-duotone fa-hourglass-half"
        :tear="section.tear"
      >
        <template #label><ResourceString for="ComingSoon" /></template>
        <ResourceString :for="section.key" as="p" class="kcc-body" />
      </KccSheet>
    </div>
  </div>
</template>
