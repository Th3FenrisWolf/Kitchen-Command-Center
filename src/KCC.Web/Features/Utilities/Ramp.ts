export type Ramp = 'light' | 'dark'

export type RampSetting = 'Device' | 'Light' | 'Dark'

export const STORED_RAMP_KEY = 'kcc-theme'

let latestSwitch = 0

export function applyRamp(ramp: Ramp) {
  const root = document.documentElement
  const thisSwitch = ++latestSwitch
  root.setAttribute('data-theme-switching', '')
  root.setAttribute('data-theme', ramp)
  // A frame's callbacks run before its style recalculation, so clearing the freeze in the first frame can leave no
  // recalculation that saw it and the palettes would interpolate. The second frame follows one that rendered with it.
  requestAnimationFrame(() =>
    requestAnimationFrame(() => {
      if (thisSwitch === latestSwitch) {
        root.removeAttribute('data-theme-switching')
      }
    }),
  )
}

export function deviceRamp(): Ramp {
  let stored: string | null = null
  try {
    stored = window.localStorage.getItem(STORED_RAMP_KEY)
  } catch {
    // Storage blocked: the OS preference still applies.
  }
  if (stored === 'light' || stored === 'dark') {
    return stored
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function rampFor(setting: RampSetting): Ramp {
  if (setting === 'Light') {
    return 'light'
  }
  if (setting === 'Dark') {
    return 'dark'
  }
  return deviceRamp()
}
