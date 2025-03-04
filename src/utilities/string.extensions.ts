declare global {
  interface String {
    equals(str: string, comparison: StringComparison): boolean
  }
}

String.prototype.equals = function (
  str: string,
  comparison: StringComparison = StringComparison.Ordinal,
): boolean {
  if (comparison === StringComparison.Ordinal) {
    return this === str
  }
  return this.toLowerCase() === str.toLowerCase()
}

export enum StringComparison {
  Ordinal,
  IgnoreCase,
}

export const IgnoreCase = StringComparison.IgnoreCase
export const Ordinal = StringComparison.Ordinal
