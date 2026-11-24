import {
  defineAsyncComponent,
  defineComponent,
  h,
  inject,
  provide,
  shallowRef,
  type Component,
  type InjectionKey,
} from 'vue'

export { registerLaterPart, scheduleLaterParts } from './laterSchedule'

type Loader = () => Promise<Component>

const ATTEMPTS = 3
const RETRY_STEP_MS = 1000
const LOAD_TIMEOUT_MS = 15_000

const RETRY_KEY: InjectionKey<() => void> = Symbol('laterPartRetry')
type AsyncInternals = { __asyncLoader: () => Promise<unknown>; __asyncResolved?: Component }

const attempts = new WeakMap<Component, () => AsyncInternals>()

export function useLaterPartRetry(): () => void {
  return inject(RETRY_KEY, () => undefined)
}

// defineAsyncComponent's own timeout shows the error state without retrying, so a hung load rejects here instead.
function withinTimeout(loader: Loader): Loader {
  return () =>
    new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error(`Later part timed out after ${LOAD_TIMEOUT_MS} ms`)), LOAD_TIMEOUT_MS)
      loader()
        .then(resolve, reject)
        .finally(() => clearTimeout(timer))
    })
}

// The pending component gets the part's own props, so it can draw the part's sheet with its tear. It is the error
// component too, given the error as a prop, so a part that never arrives fails in the same sheet. Vue keeps a failed
// load's rejection for good, so Retry starts a fresh async component.
export function laterPart(loader: Loader, pending: Component): Component {
  const attempt = () =>
    defineAsyncComponent({
      loader: withinTimeout(loader),
      loadingComponent: pending,
      errorComponent: pending,
      delay: 0,
      onError: (_error, retry, fail, attempts) =>
        attempts >= ATTEMPTS ? fail() : setTimeout(retry, attempts * RETRY_STEP_MS),
    })
  const current = shallowRef(attempt())
  const part = defineComponent({
    name: 'LaterPart',
    inheritAttrs: false,
    setup(_, { attrs, slots }) {
      provide(RETRY_KEY, () => {
        current.value = attempt()
      })
      return () => h(current.value, attrs, slots)
    },
  })
  attempts.set(part, () => current.value as unknown as AsyncInternals)
  return part
}

export function prefetchPart(part: Component): Promise<unknown> {
  return attempts.get(part)?.().__asyncLoader() ?? Promise.resolve()
}

export function arrivedPart(part: Component): Component | undefined {
  return attempts.get(part)?.().__asyncResolved
}

// Longer than three timed-out attempts and their pauses take, so a part that fails shows its error state before the
// wait gives up.
export const ARRIVAL_TIMEOUT_MS = ATTEMPTS * (LOAD_TIMEOUT_MS + ATTEMPTS * RETRY_STEP_MS)
