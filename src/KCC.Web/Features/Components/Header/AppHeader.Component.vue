<script setup lang="ts">
  import { onBeforeUnmount, onMounted, provide, ref } from 'vue'
  import MenuItem, { type NavItem } from '~/Components/Header/MenuItem.vue'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'
  import logoOnDark from '~/Components/Header/Assets/logo-on-dark.webp'
  import logoOnLight from '~/Components/Header/Assets/logo-on-light.webp'
  import ThemeToggle from '~/Components/Theme/ThemeToggle.vue'
  import type { NavModel } from '~/Types/Nav'

  export interface AppHeaderProps {
    homeUrl: string
    logoAlt: string
    switchToLightLabel: string
    switchToDarkLabel: string
    mainNavItems: NavItem[]
    utilityNavItems: NavItem[]
    nav: NavModel
  }

  const { homeUrl, logoAlt, switchToLightLabel, switchToDarkLabel, mainNavItems, utilityNavItems } =
    defineProps<AppHeaderProps>()

  const navRef = ref<HTMLElement | null>(null)
  const openId = ref<string | null>(null)

  provide(MENU_CONTROLLER_KEY, {
    openId,
    setOpen: (id) => {
      openId.value = id
    },
  })

  const handleDocumentClick = (event: MouseEvent) => {
    if (navRef.value && !navRef.value.contains(event.target as Node)) {
      openId.value = null
    }
  }

  onMounted(() => {
    document.addEventListener('click', handleDocumentClick)
  })

  onBeforeUnmount(() => {
    document.removeEventListener('click', handleDocumentClick)
  })
</script>

<template>
  <header class="content-grid mt-4">
    <nav
      ref="navRef"
      class="breakout relative flex w-full flex-wrap items-center gap-x-6 gap-y-2 border-b border-dashed border-hair-strong px-4 text-ink sm:px-6"
    >
      <a
        class="btn-no-style z-20 shrink-0 py-4 text-ink focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ink"
        :href="homeUrl"
      >
        <img
          data-ramp="dark"
          loading="eager"
          :src="logoOnDark"
          :alt="logoAlt"
          class="h-12 w-auto sm:h-16"
          height="64"
          width="90"
        />
        <img
          data-ramp="light"
          loading="eager"
          :src="logoOnLight"
          :alt="logoAlt"
          class="h-12 w-auto sm:h-16"
          height="64"
          width="90"
        />
      </a>

      <ul class="flex gap-4">
        <li v-for="(item, index) in mainNavItems" :key="item.displayText">
          <MenuItem :item :menu-id="`main-${index}-${item.displayText}`" />
        </li>
      </ul>

      <ul class="ml-auto flex items-center gap-4">
        <li v-for="(item, index) in utilityNavItems" :key="item.displayText">
          <MenuItem :item :menu-id="`utility-${index}-${item.displayText}`" />
        </li>
        <li>
          <ThemeToggle :switch-to-light-label="switchToLightLabel" :switch-to-dark-label="switchToDarkLabel" />
        </li>
      </ul>
    </nav>
  </header>
</template>
