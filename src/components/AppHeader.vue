<script setup lang="ts">
import MenuItem from '~/components/MenuItem.vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const routes = router.getRoutes()

const mainNav = routes.filter((route) => route.meta.mainNav)
const userNav = routes.filter((route) => route.meta.userNav)
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

      <MenuItem
        class="ml-auto"
        :route="userNav.find((item) => item.meta.whenAuthenticated === false)"
      />
    </nav>
  </header>
</template>
