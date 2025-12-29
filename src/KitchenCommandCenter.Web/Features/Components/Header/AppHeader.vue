<script setup lang="ts">
import MenuItem from '~/Components/Header/MenuItem.vue'

const mainNav = [
  {
    name: 'Dashboard',
    path: '/',
  },
  {
    name: 'Recipes',
    path: '/recipes',
  },
]

const userNav = [
  {
    name: 'Login',
    path: '/login',
    whenAuthenticated: false,
  },
  {
    name: 'Register',
    path: '/register',
    whenAuthenticated: false,
  },
  {
    name: 'Profile',
    path: '/profile',
    whenAuthenticated: true,
  },
  {
    name: 'Logout',
    path: '/logout',
    whenAuthenticated: true,
  },
]
</script>

<template>
  <header class="content-grid bg-base text-bone">
    <nav class="flex size-full h-16 gap-8">
      <RouterLink
        class="btn-no-style focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-bone"
        to="/"
      >
        Insert Logo Here
      </RouterLink>

      <ul class="flex">
        <li v-for="route in mainNav" :key="route.name">
          <MenuItem :route />
        </li>
      </ul>

      <MenuItem class="ml-auto" :route="userNav.find((item) => !item.whenAuthenticated)" />
    </nav>
  </header>
</template>
