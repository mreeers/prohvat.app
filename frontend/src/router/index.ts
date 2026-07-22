import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
      // Public: anyone can view the map with rides/spots
    },
    {
      path: '/feed',
      name: 'feed',
      component: () => import('../views/FeedView.vue')
    },
    {
      path: '/auth/login',
      name: 'login',
      component: () => import('../views/LoginView.vue')
    },
    {
      path: '/auth/register',
      name: 'register',
      component: () => import('../views/RegisterView.vue')
    },
    {
      path: '/rides/new',
      name: 'createRide',
      component: () => import('../views/CreateRideView.vue')
    },
    {
      // My garage (private — shows only MY vehicles + add button)
      path: '/garage',
      name: 'garage',
      component: () => import('../views/GarageView.vue'),
      meta: { requiresAuth: true }
    },
    {
      // Add new vehicle (private)
      path: '/garage/new',
      name: 'new-vehicle',
      component: () => import('../views/NewVehicleView.vue'),
      meta: { requiresAuth: true }
    },
    {
      // Public vehicle / logbook view — anyone can read
      path: '/garage/:id',
      name: 'vehicle-detail',
      component: () => import('../views/VehicleDetailView.vue')
    },
    {
      path: '/tuning',
      name: 'tuning',
      component: () => import('../views/TuningView.vue')
    },
    {
      // Public profile view
      path: '/profile/:username',
      name: 'profile',
      component: () => import('../views/ProfileView.vue')
    },
    {
      path: '/messages',
      name: 'messages',
      component: () => import('../views/MessagesView.vue'),
      meta: { requiresAuth: true }
    }
  ]
})

// Navigation guard: redirect to login for protected routes
router.beforeEach((to, _from, next) => {
  if (to.meta.requiresAuth) {
    const authStore = useAuthStore()
    if (!authStore.isAuthenticated()) {
      next({ name: 'login', query: { redirect: to.fullPath } })
      return
    }
  }
  next()
})

export default router
