<script lang="ts">
  /**
   * A header component that displays a logo and navigation items
   *
   * @param {ImageItem} logo - The logo to display
   * @param {NavItem[]} mainNavItems - The main navigation items
   * @param {NavItem[]} utilityNavItems - The utility navigation items
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

  interface NavItem {
    displayText: string
    subLinks: PageLink[]
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
    /**
     * The logo to display
     */
    logo: ImageItem
    /**
     * The main navigation items
     */
    mainNavItems: NavItem[]
    /**
     * The utility navigation items
     */
    utilityNavItems: NavItem[]
  }
</script>

<script setup lang="ts">
  import { onBeforeUnmount, onMounted, provide, ref } from 'vue'
  import MenuItem from '~/Components/Header/MenuItem.vue'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'

  const { homeUrl, logo, mainNavItems, utilityNavItems } = defineProps<AppHeaderProps>()

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
    <nav ref="navRef" class="breakout relative flex size-full items-center gap-8 rounded-3xl bg-surface-500 px-8">
      <a
        class="btn-no-style z-20 h-full shrink-0 py-4 text-bone focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-bone"
        :href="homeUrl.stripTilde()"
      >
        <img
          loading="eager"
          :src="logo.asset.url.stripTilde()"
          :alt="logo.altText"
          class="h-16 w-auto"
          height="64"
          width="90"
        />
      </a>

      <ul class="flex gap-4">
        <li v-for="(item, index) in mainNavItems" :key="item.displayText">
          <MenuItem :item :menu-id="`main-${index}-${item.displayText}`" />
        </li>
      </ul>

      <ul class="ml-auto flex gap-4">
        <li v-for="(item, index) in utilityNavItems" :key="item.displayText">
          <MenuItem :item :menu-id="`utility-${index}-${item.displayText}`" />
        </li>
      </ul>
    </nav>
  </header>
</template>
