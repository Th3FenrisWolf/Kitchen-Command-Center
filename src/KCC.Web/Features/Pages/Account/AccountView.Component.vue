<script setup lang="ts">
  import { computed } from 'vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { listTearFor } from '~/Utilities/BrandColor'
  import SignOutForm from '~/Components/Account/SignOutForm.vue'
  import Button from '~/Components/Button/Button.vue'
  import ComingSoonSection from '~/Components/ComingSoon/ComingSoonSection.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  export interface AccountViewProps {
    displayName: string
    initials: string
    memberSince: string
    settingsUrl: string
    logoutUrl: string
    recipeGroups: RecipeGroup[]
    resourceStrings?: Record<string, string>
  }

  interface ProfileVariant {
    key: string
    name: string
    icon?: string
    url?: string
    isPending: boolean
  }

  interface RecipeGroup {
    key: string
    recipeName: string
    recipeIcon?: string
    recipeUrl?: string
    isPending: boolean
    startedByYou: boolean
    variants: ProfileVariant[]
  }

  const props = defineProps<AccountViewProps>()

  provideResourceStrings(props.resourceStrings, 'Account')

  const recipesStartedCount = computed(() => props.recipeGroups.filter((group) => group.startedByYou).length)
  const variantsCount = computed(() => props.recipeGroups.reduce((sum, group) => sum + group.variants.length, 0))
  const favoritesFlips = computed(() => props.recipeGroups.length % 2 === 0)
</script>

<template>
  <div class="mt-rule-1 grid items-start gap-x-7 gap-y-rule-1.5 md:grid-cols-content-aside">
    <KccSheet
      as="section"
      wash="lavender"
      :at="{ x: '88%', y: '18%', w: '38%', h: '60%' }"
      :tear="1"
      class="md:col-start-1 md:row-start-1"
    >
      <h1 class="kcc-display">{{ displayName }}</h1>
      <p class="kcc-kick">
        <ResourceString for="MemberSince" /> <span class="kcc-num">{{ memberSince }}</span>
      </p>

      <div class="kcc-stats mt-rule-1">
        <div>
          <ResourceString for="RecipesLabel" as="p" class="kcc-lbl" />
          <p class="kcc-v">{{ recipesStartedCount }}</p>
        </div>
        <div>
          <ResourceString for="VariantsLabel" as="p" class="kcc-lbl" />
          <p class="kcc-v">{{ variantsCount }}</p>
        </div>
      </div>

      <div class="mt-rule-1 flex flex-wrap items-center gap-x-3 gap-y-rule-0.5">
        <Button as="a" :href="settingsUrl" variant="ghost">
          <ResourceString for="AccountSettings" />
        </Button>
        <SignOutForm :action="logoutUrl">
          <Button type="submit" variant="text">
            <ResourceString for="SignOut" />
          </Button>
        </SignOutForm>
      </div>
    </KccSheet>

    <section class="md:col-start-2 md:row-span-2 md:row-start-1">
      <div class="kcc-secname"><ResourceString for="MyRecipesAndVariants" as="h2" class="kcc-section" /></div>

      <ResourceString v-if="!recipeGroups.length" for="NoCreationsYet" as="p" class="kcc-body" />

      <!-- The profile sheet beside the list takes the first tear, so the list starts at the second and no two
           neighbours are torn alike. -->
      <ul v-else class="grid gap-y-rule-1.5">
        <KccSheet
          v-for="(group, index) in recipeGroups"
          :key="group.key"
          as="li"
          :tear="listTearFor(index + 1)"
          pad="16px"
          wide
        >
          <div class="flex flex-wrap items-center gap-x-3 gap-y-0">
            <i v-if="group.recipeIcon" :class="group.recipeIcon" aria-hidden="true"></i>
            <h3 class="kcc-title">
              <component
                :is="group.recipeUrl ? 'a' : 'span'"
                :href="group.recipeUrl || undefined"
                :class="group.recipeUrl && 'kcc-link'"
              >
                {{ group.recipeName }}
              </component>
            </h3>
            <span v-if="group.startedByYou" class="kcc-badge"><ResourceString for="StartedByYou" /></span>
            <span v-if="group.isPending" class="kcc-badge"><ResourceString for="PendingReview" /></span>
          </div>

          <ul v-if="group.variants.length" class="mt-rule-1 grid">
            <li v-for="variant in group.variants" :key="variant.key" class="grid grid-cols-[1fr_auto] items-center gap-3">
              <span class="flex flex-wrap items-center gap-x-2 gap-y-0">
                <i v-if="variant.icon" :class="variant.icon" aria-hidden="true"></i>
                <component
                  :is="variant.url ? 'a' : 'span'"
                  :href="variant.url || undefined"
                  :class="variant.url && 'kcc-link'"
                >
                  {{ variant.name }}
                </component>
              </span>
              <span v-if="variant.isPending" class="kcc-badge"><ResourceString for="PendingReview" /></span>
            </li>
          </ul>
        </KccSheet>
      </ul>
    </section>

    <!-- No Shared.ComingSoon/Favorites/RecentActivity keys exist, so shared resolves the whole section against this page's own Account prefix. -->
    <div class="grid gap-y-rule-1.5 md:col-start-1 md:row-start-2">
      <ComingSoonSection text-key="Favorites" :shared="false" :tear="3" :flip="favoritesFlips" />
      <ComingSoonSection text-key="RecentActivity" :shared="false" :tear="5" :flip="!favoritesFlips" />
    </div>
  </div>
</template>
