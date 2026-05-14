import { provide, type InjectionKey } from 'vue'

export interface ResourceStringsContext {
  strings: Record<string, string>
  prefix?: string
}

export const resourceStringsKey: InjectionKey<ResourceStringsContext> = Symbol('resourceStrings')

export function useResourceStrings(resourceStrings?: Record<string, string>, prefix?: string) {
  // Expose the dict + prefix to descendants so <ResourceString> can resolve keys
  // without each call site repeating the prefix.
  provide(resourceStringsKey, { strings: resourceStrings ?? {}, prefix })

  return (key: string) => {
    const fullKey = prefix ? `${prefix}.${key}` : key
    return resourceStrings?.[fullKey] ?? key
  }
}
