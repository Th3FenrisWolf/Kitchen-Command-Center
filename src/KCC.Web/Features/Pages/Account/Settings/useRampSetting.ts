import { ref } from 'vue'
import { post } from '~/Utilities/Api'
import { applyRamp, rampFor, type RampSetting } from '~/Utilities/Ramp'

export function useRampSetting(saved: RampSetting) {
  const setting = ref<RampSetting>(saved)
  const error = ref<string | null>(null)
  let confirmed = saved
  let saving = false
  let settled = Promise.resolve()

  async function saveLatest() {
    saving = true
    while (setting.value !== confirmed) {
      const sent = setting.value
      const result = await post('/api/profile/ramp', { ramp: sent })
      if (result.success) {
        confirmed = sent
      } else if (setting.value === sent) {
        setting.value = confirmed
        applyRamp(rampFor(confirmed))
        error.value = result.errorMessage
      }
    }
    saving = false
  }

  function choose(next: RampSetting) {
    if (next !== setting.value) {
      setting.value = next
      error.value = null
      applyRamp(rampFor(next))
      if (!saving) {
        settled = saveLatest()
      }
    }
    return settled
  }

  return { setting, error, choose }
}
