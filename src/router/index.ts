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
      component: () => import('~/views/RecipeSearchView.vue'),
    },
    {
      path: '/recipes/create',
      name: 'CreateRecipe',
      component: () => import('~/views/CreateRecipeView.vue'),
    },
    {
      path: '/recipes/:id',
      name: 'RecipeDetail',
      props: (route) => ({ id: route.params.id }),
      component: () => import('~/views/RecipeDetailView.vue'),
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
      meta: { userNav: true, whenAuthenticated: false },
      component: () => import('~/views/LoginView.vue'),
    },
    {
      path: '/profile',
      name: 'Profile',
      meta: { userNav: true, whenAuthenticated: true },
      component: () => import('~/views/ProfileView.vue'),
    },
  ],
})

export default router
