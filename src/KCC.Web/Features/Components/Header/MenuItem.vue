<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { cx } from '~/Utilities/CX'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'

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

  const { item, menuId } = defineProps<{
    item: NavItem
    menuId: string
  }>()

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
    class="relative z-20 flex h-full w-full cursor-pointer items-center rounded-2xl bg-bone px-4 py-2 font-casual text-2xl font-bold uppercase"
  >
    {{ item.displayText }}
  </a>
  <template v-else>
    <button
      type="button"
      @click="toggle"
      class="relative z-20 flex h-full w-full cursor-pointer items-center rounded-2xl bg-bone px-4 py-2 font-casual text-2xl font-bold uppercase"
    >
      {{ item.displayText }}
    </button>
    <div
      :class="
        cx(
          'absolute top-[calc(100%-1.5rem)] left-0 z-10 max-h-0 w-full overflow-hidden rounded-b-3xl bg-surface-500 text-bone transition-all duration-500 ease-in-out',
          isOpen ? 'max-h-96' : 'max-h-0',
        )
      "
    >
      <ul class="flex gap-8 p-8">
        <li
          v-for="subLink in item.subLinks"
          :key="subLink.displayText"
          class="basis-full rounded-2xl bg-bone text-onyx transition-all duration-300 ease-in-out will-change-transform hover:-translate-y-1 hover:shadow-bone-small"
        >
          <a class="block size-full p-4 text-center" :href="subLink.url?.stripTilde()" :target="subLink.target">
            {{ subLink.displayText }}
          </a>
        </li>
      </ul>
    </div>
  </template>
</template>
