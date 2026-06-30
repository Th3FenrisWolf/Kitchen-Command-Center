import { ref, type Ref } from 'vue'

// Minimal structural type so we don't depend on lib.dom Wake Lock typings being present.
interface WakeLockSentinelLike {
  release: () => Promise<void>
}
interface WakeLockNavigator {
  wakeLock?: { request: (type: 'screen') => Promise<WakeLockSentinelLike> }
}

export interface UseWakeLock {
  /** True while a sentinel is held. */
  isActive: Ref<boolean>
  /** Acquire the screen wake lock. Silent no-op when unsupported or denied. */
  request: () => Promise<void>
  /** Release the wake lock if held, and stop auto re-acquiring on visibility. */
  release: () => Promise<void>
}

export function useWakeLock(): UseWakeLock {
  const isActive = ref(false)
  let sentinel: WakeLockSentinelLike | null = null
  // True between request() and release(); drives visibility-based re-acquisition.
  let wanted = false

  const support = () => (navigator as unknown as WakeLockNavigator).wakeLock

  const acquire = async () => {
    const api = support()
    if (!api || sentinel) return
    try {
      sentinel = await api.request('screen')
      isActive.value = true
    } catch {
      // Unsupported / denied / transient failure: stay silent.
      sentinel = null
      isActive.value = false
    }
  }

  const drop = async () => {
    const current = sentinel
    sentinel = null
    isActive.value = false
    if (current) {
      try {
        await current.release()
      } catch {
        // Ignore release failures.
      }
    }
  }

  const onVisibilityChange = () => {
    if (wanted && document.visibilityState === 'visible') {
      void acquire()
    } else if (document.visibilityState === 'hidden') {
      void drop()
    }
  }

  document.addEventListener('visibilitychange', onVisibilityChange)

  const request = async () => {
    wanted = true
    await acquire()
  }

  const release = async () => {
    wanted = false
    document.removeEventListener('visibilitychange', onVisibilityChange)
    await drop()
  }

  return { isActive, request, release }
}
