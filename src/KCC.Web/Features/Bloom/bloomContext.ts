import { inject, warn, type InjectionKey } from 'vue'
import { arrivals, isGlobalKey, pageOfKey, visitStarts, type CollectedWash } from './washes'

export interface Bloom {
  readonly seed: number
  readonly page: string
  claimKey(key: string): string
  releaseKey(claimed: string): void
  collect(wash: CollectedWash): void
  collected(): readonly CollectedWash[]
  lag(): number
  lateStarts(count: number): number[] | undefined
  visitStarts(key: string, count: number, slots?: readonly number[]): number[] | undefined
  markHydrated(pageWashes?: number): void
  replay(keys: readonly string[]): Bloom
}

export interface BloomOptions {
  collect?: boolean
}

export const BLOOM_KEY: InjectionKey<Bloom> = Symbol('kcc-bloom')

const withoutRepeat = (claimed: string) => claimed.replace(/~\d+$/, '')

export function createBloom(seed: number, page: string, { collect = true }: BloomOptions = {}): Bloom {
  const held = new Set<string>()
  const collected: CollectedWash[] = []
  let hydrated = false
  let nextLateStart: (() => number) | undefined

  const qualify = (key: string) => (isGlobalKey(key) ? key : `${page}|${key}`)

  const bloom: Bloom = {
    seed,
    page,
    claimKey(key) {
      const qualified = qualify(key)
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
    visitStarts: (key, count, slots) => (hydrated ? visitStarts(seed, key, count, slots) : undefined),
    markHydrated(pageWashes = collected.filter((wash) => !isGlobalKey(wash.key)).length) {
      if (hydrated) return
      hydrated = true
      nextLateStart = arrivals(seed, pageOfKey(`${page}|`), pageWashes)
      for (let i = 0; i < pageWashes; i++) nextLateStart()
    },
    // Server-only sheets claim keys the client never sees, so an island must take the exact keys, suffixes included,
    // that the server gave it, or its washes would land on other sheets' hashes.
    replay(keys) {
      const pending = [...keys]
      for (const key of pending) held.add(key)
      return {
        ...bloom,
        claimKey(key) {
          const qualified = qualify(key)
          const at = pending.findIndex((claimed) => withoutRepeat(claimed) === qualified)
          return at < 0 ? bloom.claimKey(key) : pending.splice(at, 1)[0]
        },
      }
    },
  }

  return bloom
}

export function useBloom(): Bloom {
  return inject(BLOOM_KEY, () => createBloom(0, '', { collect: false }), true)
}
