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
      path: '/recipe/:id',
      name: 'RecipeDetail',
      props: (route) => ({ id: Number(route.params.id) }),
      component: () => import('~/views/RecipeDetail.vue'),
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
