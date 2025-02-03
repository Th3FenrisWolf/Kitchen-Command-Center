import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

export const useCounterStore = defineStore('counter', () => {
  const count = ref(0)
  const doubleCount = computed(() => count.value * 2)
  function increment() {
    count.value += 1
  }

  return { count, doubleCount, increment }
})

// * ref(): state property
// * computed(): getter
// * function(): action
// * const { count, doubleCount } = storeToRefs(store)
// * const { increment } = store

export default useCounterStore
