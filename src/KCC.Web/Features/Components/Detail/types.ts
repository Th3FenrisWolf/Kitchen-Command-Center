export interface StatTileSpec {
  /** Font Awesome class for the top icon (ignored when `dotColor` is set). */
  icon?: string
  /** Design-token name for a status dot instead of an icon (e.g. 'green'). */
  dotColor?: string
  /** The big value. `null`/`undefined` renders as an em dash. */
  value?: string | number | null
  /** Small unit suffix shown after the value (e.g. 'min'). */
  unit?: string
  /** Caption under the value. */
  label: string
  /** When true, render `value` as a muted "coming soon" badge. */
  comingSoon?: boolean
}
