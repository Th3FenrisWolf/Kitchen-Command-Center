<script setup lang="ts">
import MenuItem from '~/components/MenuItem.vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const routes = router.getRoutes()
const loginRoute = routes.pop()
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
        <li v-for="route in routes" :key="route.name">
          <MenuItem :route />
        </li>
      </ul>
      <MenuItem v-if="loginRoute" :route="loginRoute" class="ml-auto" />
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
