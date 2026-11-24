type Prefetch = () => unknown

// Safari has no requestIdleCallback; a short timeout still lets the first frames after usable go first.
const IDLE_FALLBACK_MS = 200

const waiting = new Set<Prefetch>()
let scheduled = false
let idleQueued = false

function whenIdle(run: () => void): void {
  if (typeof requestIdleCallback === 'function') requestIdleCallback(run)
  else setTimeout(run, IDLE_FALLBACK_MS)
}

function prefetchQuietly(prefetch: Prefetch): void {
  // A part that fails to prefetch loads again when a reader opens it.
  try {
    void Promise.resolve(prefetch()).catch(() => undefined)
  } catch {
    return
  }
}

function prefetchWaiting(): void {
  idleQueued = false
  const due = [...waiting]
  waiting.clear()
  due.forEach(prefetchQuietly)
}

function queueIdle(): void {
  if (idleQueued) return
  idleQueued = true
  whenIdle(prefetchWaiting)
}

// The sidecar never schedules, so a part registered there would wait for the life of the process.
export function registerLaterPart(prefetch: Prefetch): void {
  if (import.meta.env.SSR) return
  waiting.add(prefetch)
  if (scheduled) queueIdle()
}

export function scheduleLaterParts(): void {
  scheduled = true
  queueIdle()
}
