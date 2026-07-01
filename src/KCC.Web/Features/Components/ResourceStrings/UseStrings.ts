import { inject, provide, type InjectionKey } from 'vue'

export interface ResourceStringsContext {
  strings: Record<string, string>
  prefix?: string
}

export const resourceStringsKey: InjectionKey<ResourceStringsContext> = Symbol('resourceStrings')

function resolve(ctx: ResourceStringsContext, key: string): string {
  const fullKey = ctx.prefix ? `${ctx.prefix}.${key}` : key
  return ctx.strings[fullKey] ?? fullKey
}

export function useResourceStrings(resourceStrings?: Record<string, string>, prefix?: string) {
  // Expose the dict + prefix to descendants so <ResourceString> can resolve keys
  // without each call site repeating the prefix.
  const ctx: ResourceStringsContext = { strings: resourceStrings ?? {}, prefix }
  provide(resourceStringsKey, ctx)
  return (key: string) => resolve(ctx, key)
}

/** Consume-only resolver for descendants (e.g. to fill placeholder/title attributes). */
export function useStrings() {
  const ctx = inject(resourceStringsKey, { strings: {} })
  return (key: string) => resolve(ctx, key)
}
