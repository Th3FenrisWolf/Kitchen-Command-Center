const MIN_RULES = 2

export interface DragInput {
  startRules: number
  floor: number
  deltaY: number
  rulePx: number
}

export function draggedRules({ startRules, floor, deltaY, rulePx }: DragInput): number {
  return Math.max(MIN_RULES, floor, startRules + Math.round(deltaY / rulePx))
}
