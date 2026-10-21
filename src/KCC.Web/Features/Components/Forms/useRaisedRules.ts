import { ref, watch } from 'vue'

export function useRaisedRules(minRules: () => number) {
  const rules = ref(minRules())
  watch(minRules, (next) => {
    rules.value = Math.max(rules.value, next)
  })
  return rules
}
