export type NavLabel = (key: string, ...values: (string | number)[]) => string

export function navLabels(labels: Record<string, string>): NavLabel {
  return (key, ...values) =>
    (labels[`Nav.${key}`] ?? `Nav.${key}`).replace(/\{(\d+)\}/g, (placeholder, index: string) =>
      index in values ? String(values[Number(index)]) : placeholder,
    )
}
