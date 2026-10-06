<script setup lang="ts">
  import { computed } from 'vue'
  import SignOutForm from '~/Components/Account/SignOutForm.vue'
  import type { NavMember, NavUrls } from '~/Types/Nav'
  import PadLines from './PadLines.vue'
  import { isCurrentPage } from './isCurrentPage'
  import { kitchenLines } from './kitchenLines'
  import type { NavLabel } from './navLabels'

  export interface KitchenCardProps {
    member: NavMember
    urls: NavUrls
    t: NavLabel
  }

  const { member, urls, t } = defineProps<KitchenCardProps>()

  const lines = computed(() => kitchenLines(member, urls, t))
</script>

<template>
  <div class="pad-kitchen">
    <div>
      <p class="kcc-kick" data-row style="--i: 0">{{ t('SignedInAs') }}</p>
      <p class="kcc-h4" data-row style="--i: 1">
        <a
          v-if="urls.account"
          class="kcc-link"
          :href="urls.account"
          :aria-current="isCurrentPage(urls.account, urls.currentPage) ? 'page' : undefined"
          >{{ member.firstName }}</a
        >
        <template v-else>{{ member.firstName }}</template>
      </p>
      <p class="kcc-kick" data-row style="--i: 2">
        {{ t('MemberSince') }} <span class="kcc-num">{{ member.memberSince }}</span>
      </p>
      <div v-if="member.kitchen" class="kcc-stats mt-6" data-row style="--i: 3">
        <div>
          <p class="kcc-lbl">{{ t('Recipes') }}</p>
          <p class="kcc-v">{{ member.kitchen.recipes }}</p>
        </div>
        <div>
          <p class="kcc-lbl">{{ t('Variants') }}</p>
          <p class="kcc-v">{{ member.kitchen.variants }}</p>
        </div>
      </div>
    </div>
    <PadLines :rows="lines" :from="1" glyphs :current-page="urls.currentPage" />
  </div>

  <template v-if="urls.signOut">
    <hr class="kcc-hr" data-row style="--i: 6" />
    <div class="pad-foot pad-foot--end" data-row style="--i: 7">
      <SignOutForm :action="urls.signOut">
        <button type="submit" class="kcc-btn kcc-btn--text">{{ t('SignOut') }}</button>
      </SignOutForm>
    </div>
  </template>
</template>

<style>
  .pad-kitchen {
    display: grid;
    gap: 0 28px;
    grid-template-columns: 1fr 1.2fr;
  }

  .pad-kitchen .kcc-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
</style>
