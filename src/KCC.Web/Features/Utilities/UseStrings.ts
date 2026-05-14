export function useResourceStrings(resourceStrings?: Record<string, string>, prefix?: string) {
  return (key: string) => {
    const fullKey = prefix ? `${prefix}.${key}` : key
    return resourceStrings?.[fullKey] ?? key
  }
}
