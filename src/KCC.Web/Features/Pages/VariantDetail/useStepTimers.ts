export interface TimerSpec {
  /** Stable index within the step's parsed matches; used as a list key. */
  id: number
  /** Countdown length in seconds (upper bound for ranges). */
  seconds: number
  /** Original matched phrase, shown as the chip label, e.g. "10-12 minutes". */
  label: string
}

// number, optional range (-, en-dash, em-dash, or " to "), then a unit.
// Capture groups: 1 = first number, 2 = optional upper bound (only after a range token), 3 = unit.
// The upper-bound group lives inside the range group so an unbroken digit run can't backtrack super-linearly.
const DURATION_RE = /(\d+(?:\.\d+)?)(?:\s*(?:[-–—]|\s+to\s+)\s*(\d+(?:\.\d+)?))?\s*(hours?|hrs?|minutes?|mins?|min|hr)\b/gi

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
    const amount = Math.max(lower, upper)
    const seconds = Math.round(amount * unitToSeconds(match[3]!))
    specs.push({ id: specs.length, seconds, label: match[0]!.trim() })
  }
  return specs
}

/**
 * Seconds left until `targetEndMs`, computed from `now` (defaults to Date.now()).
 * Timestamp-based so backgrounded tabs read the correct value on resume.
 * Ceils to whole seconds and clamps at zero.
 */
export function remainingSeconds(targetEndMs: number, now: number = Date.now()): number {
  return Math.max(0, Math.ceil((targetEndMs - now) / 1000))
}
