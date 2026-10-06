<script setup lang="ts">
  import { computed } from 'vue'
  import SignOutForm from '~/Components/Account/SignOutForm.vue'
  import type { NavMember, NavRecipes, NavRow, NavUrls } from '~/Types/Nav'
  import PadLines from './PadLines.vue'
  import { isCurrentPage } from './isCurrentPage'
  import { kitchenLines } from './kitchenLines'
  import type { NavLabel } from './navLabels'

  export interface MenuCardProps {
    recipes: NavRecipes
    urls: NavUrls
    member?: NavMember
    t: NavLabel
  }

  const { recipes, urls, member, t } = defineProps<MenuCardProps>()

  const kitchen = computed<NavRow[]>(() => {
    if (!member) {
      return []
    }
    const name = urls.account ? [{ label: member.firstName, url: urls.account, icon: 'fa-duotone fa-user-chef' }] : []
    return [...name, ...kitchenLines(member, urls, t)]
  })
</script>

<template>
  <template v-if="recipes.meals.length">
    <p class="kcc-kick" data-row style="--i: 0">{{ t('Meals') }}</p>
    <PadLines :rows="recipes.meals" :from="1" glyphs two-columns :current-page="urls.currentPage" />
  </template>

  <template v-if="recipes.diets.length">
    <p class="kcc-kick mt-6" data-row style="--i: 4">{{ t('Diets') }}</p>
    <div class="kcc-badges pad-chips" data-row style="--i: 4">
      <a
        v-for="diet in recipes.diets"
        :key="diet.url"
        class="kcc-badge pad-chip"
        :href="diet.url"
        :aria-current="isCurrentPage(diet.url, urls.currentPage) ? 'page' : undefined"
        >{{ diet.label }}</a
      >
    </div>
  </template>

  <template v-if="recipes.quickPicks.length">
    <p class="kcc-kick mt-6" data-row style="--i: 5">{{ t('QuickPicks') }}</p>
    <PadLines :rows="recipes.quickPicks" :from="5" glyphs :current-page="urls.currentPage" />
  </template>

  <hr class="kcc-hr" data-row style="--i: 7" />

  <template v-if="member">
    <p class="kcc-kick" data-row style="--i: 7">{{ t('KitchenOf', member.firstName) }}</p>
    <PadLines :rows="kitchen" :from="7" glyphs :current-page="urls.currentPage" />
    <div class="pad-foot mt-6" data-row style="--i: 7">
      <a v-if="urls.newRecipe" class="kcc-btn" :href="urls.newRecipe">
        <i class="fa-duotone fa-plus" aria-hidden="true"></i>{{ t('NewRecipe') }}
      </a>
      <SignOutForm v-if="urls.signOut" :action="urls.signOut">
        <button type="submit" class="kcc-btn kcc-btn--text">{{ t('SignOut') }}</button>
      </SignOutForm>
    </div>
  </template>
  <div v-else class="pad-foot" data-row style="--i: 7">
    <a v-if="urls.signIn" class="kcc-btn kcc-btn--ghost" :href="urls.signIn">
      <i class="fa-duotone fa-right-to-bracket" aria-hidden="true"></i>{{ t('SignIn') }}
    </a>
    <a v-if="urls.register" class="kcc-link kcc-kick" :href="urls.register">{{ t('AskForAnAccount') }}</a>
  </div>
</template>

<style>
  /* The kit's badges are 22px tall, so each row of chips takes 24px: the rule. */
  .pad-chips {
    padding-bottom: 2px;
    row-gap: 2px;
  }
</style>
