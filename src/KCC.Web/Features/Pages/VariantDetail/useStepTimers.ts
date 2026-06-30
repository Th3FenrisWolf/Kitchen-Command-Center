export interface TimerSpec {
  /** Stable index within the step's parsed matches; used as a list key. */
  id: number
  /** Countdown length in seconds (upper bound for ranges). */
  seconds: number
  /** Original matched phrase, shown as the chip label, e.g. "10-12 minutes". */
  label: string
}

// number, optional range (-, en-dash, em-dash, or " to "), then a unit.
// Capture groups: 1 = first number, 2 = optional upper number, 3 = unit.
const DURATION_RE = /(\d+(?:\.\d+)?)\s*(?:[-–—]|\s+to\s+)?\s*(\d+(?:\.\d+)?)?\s*(hours?|hrs?|minutes?|mins?|min|hr)\b/gi

function unitToSeconds(unit: string): number {
  return /^h/i.test(unit) ? 3600 : 60
}

/** Parse all durations in a step's text into TimerSpecs. Empty array when none. */
export function parseDurations(text: string): TimerSpec[] {
  const specs: TimerSpec[] = []
  let match: RegExpExecArray | null
  // Fresh regex state per call (the literal carries the global flag).
  DURATION_RE.lastIndex = 0
  while ((match = DURATION_RE.exec(text)) !== null) {
    const lower = Number.parseFloat(match[1]!)
    const upper = match[2] != null ? Number.parseFloat(match[2]) : lower
    const amount = Number.isNaN(upper) ? lower : Math.max(lower, upper)
    const seconds = Math.round(amount * unitToSeconds(match[3]!))
    specs.push({ id: specs.length, seconds, label: match[0]!.trim() })
  }
  return specs
}
