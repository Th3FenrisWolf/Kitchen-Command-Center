<script setup lang="ts">
import MenuItem from '~/components/MenuItem.vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const routes = router.getRoutes()
const mainNav = routes.filter((route) => route.meta.mainNav)
const utilityNav = routes.filter((route) => route.meta.utilityNav)
</script>

<template>
  <header class="bg-base text-bone">
    <nav class="flex size-full h-16 gap-8">
      <RouterLink
        class="btn-no-style focus-visible:outline-bone focus-visible:outline-2 focus-visible:-outline-offset-2"
        to="/"
      >
        Insert Logo Here
      </RouterLink>

      <ul class="flex">
        <li v-for="route in mainNav" :key="route.name">
          <MenuItem :route />
        </li>
      </ul>

      <ul class="ml-auto flex">
        <li v-for="route in utilityNav" :key="route.name">
          <MenuItem :route />
        </li>
      </ul>
    </nav>
  </header>
</template>

<style scoped>
.login:has(.btn-login:focus-visible) {
  outline: 2px solid var(--color-bone);
  outline-offset: -2px;
}

.btn-login:focus-visible {
  outline: none;
}
</style>
