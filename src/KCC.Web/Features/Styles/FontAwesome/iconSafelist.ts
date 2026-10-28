import { readFileSync } from 'node:fs'

// The built stylesheet ships a rule only for these icons, so an icon missing here renders blank.
const interfaceIcons = `
  arrow-left arrow-right arrows-rotate bars basket-shopping bolt book-open camera check chevron-down circle
  circle-half-stroke circle-notch clipboard-check clock clock-rotate-left comment-pen dice eye eye-slash
  file-circle-plus fire-burner gauge gear grip-lines hourglass hourglass-half house images key keyboard layer-group
  list list-check list-ol magnifying-glass minus pause pen pen-nib pen-to-square plate-utensils play plus
  right-from-bracket right-to-bracket rotate-left rotate-right sliders sparkles star star-half-stroke stopwatch
  table-cells-large triangle-exclamation user users utensils xmark
`

const recipeIconsSource = readFileSync(new URL('../../../../KCC.Admin/RecipeIcons.cs', import.meta.url), 'utf8')
const recipeIcons = recipeIconsSource.match(/All = """([\s\S]*?)"""/)?.[1]
if (!recipeIcons) throw new Error('RecipeIcons.All not found in KCC.Admin/RecipeIcons.cs')

const names = (list: string) => list.split(/\s+/).filter(Boolean)

export const keptIcons: ReadonlySet<string> = new Set([...names(interfaceIcons), ...names(recipeIcons)])

export const keptUtilities: ReadonlySet<string> = new Set(['spin'])
