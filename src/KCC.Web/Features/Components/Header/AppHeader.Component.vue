<!-- #region AppHeader Component Properties -->
<script lang="ts">
  import { onBeforeUnmount, onMounted, provide, ref } from 'vue'
  import MenuItem from '~/Components/Header/MenuItem.vue'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'

  /**
   * Site header: logo, main navigation, and a right-aligned utility navigation.
   */
  export default {
    name: 'AppHeader',
  }

  interface ImageItem {
    asset: {
      url: string
    }
    altText: string
  }

  /**
   * A header entry that is either a dropdown group (subLinks set) or a flat link
   * (url + target set). The two modes are mutually exclusive on the server side
   * (HeaderNavItem in C# is populated from either a NavItem or a NavLink).
   */
  interface NavItem {
    displayText: string
    url?: string
    target?: string
    subLinks?: PageLink[]
  }

  interface PageLink {
    displayText: string
    url: string
    target: string
  }

  export interface AppHeaderProps {
    /**
     * URL the logo links to. Pre-resolved via Razor's Url.Content so the server
     * HTML and hydration JSON both carry the same decorated value in preview.
     */
    homeUrl: string
    logo: ImageItem

    /**
     * Mark shown on the light ramp. When omitted, `logo` shows on both ramps.
     */
    logoLight?: ImageItem

    /** Theme-toggle labels, resolved server-side so they work on every page. */
    switchToLightLabel: string

    switchToDarkLabel: string
    mainNavItems: NavItem[]
    /**
     * Pushed to the right of the bar, after the main items.
     */
    utilityNavItems: NavItem[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import ThemeToggle from '~/Components/Theme/ThemeToggle.vue'
  const { homeUrl, logo, logoLight, switchToLightLabel, switchToDarkLabel, mainNavItems, utilityNavItems } =
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
        :href="homeUrl.stripTilde()"
      >
        <img
          :data-ramp="logoLight ? 'dark' : undefined"
          loading="eager"
          :src="logo.asset.url.stripTilde()"
          :alt="logo.altText"
          class="h-12 w-auto sm:h-16"
          height="64"
          width="90"
        />
        <img
          v-if="logoLight"
          data-ramp="light"
          loading="eager"
          :src="logoLight.asset.url.stripTilde()"
          :alt="logoLight.altText"
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
