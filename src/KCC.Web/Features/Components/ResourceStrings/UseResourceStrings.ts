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

export function provideResourceStrings(resourceStrings?: Record<string, string>, prefix?: string) {
  const ctx: ResourceStringsContext = { strings: resourceStrings ?? {}, prefix }
  provide(resourceStringsKey, ctx)
  return (key: string) => resolve(ctx, key)
}

export function useResourceStrings() {
  const ctx = inject(resourceStringsKey, { strings: {} })
  return (key: string) => resolve(ctx, key)
}
