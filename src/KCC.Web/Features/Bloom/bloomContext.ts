import { inject, warn, type InjectionKey } from 'vue'
import { arrivals, isGlobalKey, pageOfKey, type CollectedWash } from './washes'

export interface Bloom {
  readonly seed: number
  readonly page: string
  claimKey(key: string): string
  releaseKey(claimed: string): void
  collect(wash: CollectedWash): void
  collected(): readonly CollectedWash[]
  lag(): number
  lateStarts(count: number): number[] | undefined
  markHydrated(): void
}

export interface BloomOptions {
  collect?: boolean
}

export const BLOOM_KEY: InjectionKey<Bloom> = Symbol('kcc-bloom')

export function createBloom(seed: number, page: string, { collect = true }: BloomOptions = {}): Bloom {
  const held = new Set<string>()
  const collected: CollectedWash[] = []
  let hydrated = false
  let nextLateStart: (() => number) | undefined

  return {
    seed,
    page,
    claimKey(key) {
      const qualified = isGlobalKey(key) ? key : `${page}|${key}`
      let claimed = qualified
      for (let repeat = 2; held.has(claimed); repeat++) claimed = `${qualified}~${repeat}`
      held.add(claimed)
      if (claimed !== qualified)
        warn(`Wash key "${qualified}" repeats on this page; using ${claimed.slice(qualified.length)}.`)
      return claimed
    },
    releaseKey(claimed) {
      held.delete(claimed)
    },
    collect(wash) {
      if (collect && !hydrated) collected.push(wash)
    },
    collected: () => collected,
    lag: () => (hydrated ? performance.now() / 1000 : 0),
    lateStarts(count) {
      if (!nextLateStart) return undefined
      return Array.from({ length: count }, nextLateStart)
    },
    markHydrated() {
      if (hydrated) return
      hydrated = true
      const pageWashes = collected.filter((wash) => !isGlobalKey(wash.key)).length
      nextLateStart = arrivals(seed, pageOfKey(`${page}|`), pageWashes)
      for (let i = 0; i < pageWashes; i++) nextLateStart()
    },
  }
}

export function useBloom(): Bloom {
  return inject(BLOOM_KEY, () => createBloom(0, '', { collect: false }), true)
}
