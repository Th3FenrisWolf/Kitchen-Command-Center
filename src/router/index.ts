import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'Dashboard',
      component: () => import('~/views/DashboardView.vue'),
    },
    {
      path: '/about',
      name: 'About',
      component: () => import('~/views/AboutView.vue'),
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('~/views/LoginView.vue'),
    },
  ],
})

export default router
