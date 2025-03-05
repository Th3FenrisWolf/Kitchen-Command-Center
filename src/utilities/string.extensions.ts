declare global {
  interface String {
    equals(str?: string, comparison?: StringComparison): boolean
  }
}

String.prototype.equals = function (
  str?: string,
  comparison: StringComparison = StringComparison.Ordinal,
): boolean {
  if (typeof this !== 'string' || typeof str !== 'string') {
    return false
  }

  if (comparison === StringComparison.IgnoreCase) {
    return this.toLowerCase() === str.toLowerCase()
  }

  return this === str
}

export enum StringComparison {
  Ordinal,
  IgnoreCase,
}

export const IgnoreCase = StringComparison.IgnoreCase
export const Ordinal = StringComparison.Ordinal
