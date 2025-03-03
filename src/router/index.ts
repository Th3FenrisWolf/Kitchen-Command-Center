import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'Dashboard',
      meta: { mainNav: true },
      component: () => import('~/views/DashboardView.vue'),
    },
    {
      path: '/recipes',
      name: 'Recipes',
      meta: { mainNav: true },
      component: () => import('~/views/RecipeSearch.vue'),
    },
    {
      path: '/about',
      name: 'About',
      meta: { mainNav: true },
      component: () => import('~/views/AboutView.vue'),
    },
    {
      path: '/login',
      name: 'Login',
      meta: { utilityNav: true },
      component: () => import('~/views/LoginView.vue'),
    },
  ],
})

export default router
