<script setup lang="ts">
import { ref } from 'vue'
import cx from '~/Utilities/CX'

interface PageLink {
  displayText: string
  url: string
  target: string
}

interface NavItem {
  displayText: string
  subLinks: PageLink[]
}

const { item } = defineProps<{
  item: NavItem
}>()

const showDrawer = ref(false)
</script>
<template>
  <button
    type="button"
    @click="showDrawer = !showDrawer"
    class="relative z-20 flex h-full w-full cursor-pointer items-center rounded-2xl bg-bone px-4 py-2 font-casual text-2xl font-bold uppercase"
  >
    {{ item.displayText }}
  </button>
  <div
    :class="
      cx(
        'absolute top-[calc(100%-1.5rem)] left-0 z-10 max-h-0 w-full overflow-hidden rounded-b-3xl bg-base text-bone transition-all duration-500 ease-in-out',
        showDrawer ? 'max-h-96' : 'max-h-0',
      )
    "
  >
    <ul class="flex gap-8 p-8">
      <li
        v-for="subLink in item.subLinks"
        :key="subLink.displayText"
        class="basis-full rounded-2xl bg-bone text-onyx transition-all duration-300 ease-in-out will-change-transform hover:-translate-y-1 hover:shadow-bone-small"
      >
        <a
          class="block size-full p-4 text-center"
          :href="subLink.url.stripTilde()"
          :target="subLink.target"
        >
          {{ subLink.displayText }}
        </a>
      </li>
    </ul>
  </div>
</template>
