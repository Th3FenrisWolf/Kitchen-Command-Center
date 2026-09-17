<!-- #region MenuItem Component Properties -->
<script lang="ts">
  import { computed, inject } from 'vue'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'

  /**
   * One header entry: a flat link, or a button opening a panel of sub-links.
   */
  export default {
    name: 'MenuItem',
  }

  interface PageLink {
    displayText: string
    url: string
    target: string
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

  export interface MenuItemProps {
    item: NavItem
    /**
     * Identifies this item to the header's shared open-menu controller, so opening one closes the rest.
     */
    menuId: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { item, menuId } = defineProps<MenuItemProps>()

  const controller = inject(MENU_CONTROLLER_KEY)

  const isOpen = computed(() => controller?.openId.value === menuId)

  const toggle = () => {
    controller?.setOpen(isOpen.value ? null : menuId)
  }
</script>
<template>
  <a
    v-if="item.url"
    :href="item.url.stripTilde()"
    :target="item.target"
    class="relative z-20 flex h-full w-full cursor-pointer items-center px-4 py-2 font-sono text-[10.5px] leading-6 tracking-[.1em] text-ink uppercase decoration-hair-strong underline-offset-[3px] hover:underline"
  >
    {{ item.displayText }}
  </a>
  <template v-else>
    <button
      type="button"
      @click="toggle"
      :class="[
        'relative z-20 flex h-full w-full cursor-pointer items-center font-sono text-[10.5px] leading-6 tracking-[.1em] uppercase',
        isOpen
          ? 'rounded-md bg-marker px-3 py-2 text-marker-ink'
          : 'px-4 py-2 text-ink decoration-hair-strong underline-offset-[3px] hover:underline',
      ]"
    >
      {{ item.displayText }}
    </button>
    <div
      :class="[
        'absolute top-[calc(100%-1.5rem)] left-0 z-10 max-h-0 w-full overflow-hidden bg-paper text-ink transition-all duration-500',
        isOpen ? 'max-h-96' : 'max-h-0',
      ]"
    >
      <ul class="flex gap-8 p-8">
        <li
          v-for="subLink in item.subLinks"
          :key="subLink.displayText"
          class="basis-full rounded-md bg-paper-2 text-ink transition-all will-change-transform hover:-translate-y-1"
        >
          <a class="block size-full p-4 text-center" :href="subLink.url?.stripTilde()" :target="subLink.target">
            {{ subLink.displayText }}
          </a>
        </li>
      </ul>
    </div>
  </template>
</template>
