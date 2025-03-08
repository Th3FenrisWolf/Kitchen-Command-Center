import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { User } from 'firebase/auth'

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null)

  const isAuthenticated = computed(() => {
    return user.value !== null
  })

  const setUser = (newUser: User | null) => {
    user.value = newUser
  }

  const clearUser = () => {
    user.value = null
  }

  return { user, setUser, clearUser, isAuthenticated }
})

// * ref(): state property
// * computed(): getter
// * function(): action
// * const { count, doubleCount } = storeToRefs(store)
// * const { increment } = store

export default useUserStore
