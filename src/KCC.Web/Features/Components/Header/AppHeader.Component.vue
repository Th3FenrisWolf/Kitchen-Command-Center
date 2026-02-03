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

interface PageLink {
  displayText: string
  url: string
  target: string
}

interface NavItem {
  displayText: string
  subLinks: PageLink[]
}

export interface AppHeaderProps {
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
import MenuItem from '~/Components/Header/MenuItem.vue'

const { logo, mainNavItems, utilityNavItems } = defineProps<AppHeaderProps>()
</script>

<template>
  <header class="content-grid mt-4">
    <nav class="breakout relative flex size-full items-center gap-8 rounded-3xl bg-base px-8">
      <a
        class="btn-no-style z-20 h-full shrink-0 py-4 text-bone focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-bone"
        href="/"
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

      <ul class="flex">
        <li v-for="item in mainNavItems" :key="item.displayText">
          <MenuItem :item />
        </li>
      </ul>

      <ul class="ml-auto flex h-full">
        <li v-for="item in utilityNavItems" :key="item.displayText">
          <MenuItem :item />
        </li>
      </ul>
    </nav>
  </header>
</template>
