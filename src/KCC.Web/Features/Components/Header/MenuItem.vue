<script setup lang="ts">
  import { computed, inject } from 'vue'
  import SignOutForm from '~/Components/Account/SignOutForm.vue'
  import { isSignOutUrl } from '~/Components/Account/signOut'
  import { MENU_CONTROLLER_KEY } from '~/Components/Header/menuController'

  export interface MenuItemProps {
    item: NavItem
    /**
     * Identifies this item to the header's shared open-menu controller, so opening one closes the rest.
     */
    menuId: string
  }

  interface PageLink {
    displayText: string
    url: string
    target: string
  }

  /**
   * A header entry that is either a dropdown group (subLinks set) or a flat link
   * (url + target set). The two modes are mutually exclusive on the server side
   * (HeaderNavItem in C# is populated from either a NavGroup or a NavLink).
   */
  interface NavItem {
    displayText: string
    url?: string
    target?: string
    subLinks?: PageLink[]
  }

  const { item, menuId } = defineProps<MenuItemProps>()

  const controller = inject(MENU_CONTROLLER_KEY)

  const isOpen = computed(() => controller?.openId.value === menuId)

  // menuId carries the CMS display text, so it can hold spaces and punctuation. `aria-controls` is a
  // space-separated IDREF list, so an id with whitespace in it would resolve to nothing.
  const panelId = computed(() => `menu-${menuId.replace(/[^\w-]+/g, '-')}`)

  const toggle = () => {
    controller?.setOpen(isOpen.value ? null : menuId)
  }
</script>
<template>
  <SignOutForm v-if="isSignOutUrl(item.url)" :action="item.url">
    <button
      type="submit"
      class="kcc-kick relative z-20 flex h-full w-full cursor-pointer items-center px-4 py-2 text-ink decoration-hair-strong underline-offset-[3px] hover:underline"
    >
      {{ item.displayText }}
    </button>
  </SignOutForm>
  <a
    v-else-if="item.url"
    :href="item.url"
    :target="item.target"
    class="kcc-kick relative z-20 flex h-full w-full cursor-pointer items-center px-4 py-2 text-ink decoration-hair-strong underline-offset-[3px] hover:underline"
  >
    {{ item.displayText }}
  </a>
  <template v-else>
    <button
      type="button"
      aria-haspopup="true"
      :aria-expanded="isOpen"
      :aria-controls="panelId"
      @click="toggle"
      :class="[
        'kcc-kick relative z-20 flex h-full w-full cursor-pointer items-center px-3 py-2',
        isOpen ? 'kcc-pill' : 'text-ink decoration-hair-strong underline-offset-[3px] hover:underline',
      ]"
    >
      {{ item.displayText }}
    </button>
    <div
      :id="panelId"
      :inert="!isOpen"
      :class="[
        'absolute top-full left-0 z-10 mt-1.5 w-full overflow-hidden pb-4 transition-all duration-500',
        isOpen ? 'max-h-96' : 'max-h-0',
      ]"
    >
      <div class="kcc-slip kcc-torn kcc-tear-4" style="--r: 0">
        <div class="kcc-sheet" style="--pad: 24px">
          <ul class="flex gap-8">
            <li
              v-for="subLink in item.subLinks"
              :key="subLink.displayText"
              class="basis-full rounded-md bg-paper-2 text-ink"
            >
              <SignOutForm v-if="isSignOutUrl(subLink.url)" :action="subLink.url">
                <button
                  type="submit"
                  class="block size-full cursor-pointer p-4 text-center decoration-hair-strong underline-offset-[3px] hover:underline"
                >
                  {{ subLink.displayText }}
                </button>
              </SignOutForm>
              <a
                v-else
                class="block size-full p-4 text-center decoration-hair-strong underline-offset-[3px] hover:underline"
                :href="subLink.url"
                :target="subLink.target"
              >
                {{ subLink.displayText }}
              </a>
            </li>
          </ul>
        </div>
      </div>
    </div>
  </template>
</template>
