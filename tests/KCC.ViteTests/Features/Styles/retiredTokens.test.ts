import { readdirSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// Retired outright, no alias layer: the pre-sketch tokens (bone / onyx / surface / overlay), the Softbound kit
// (sk-*, v-ink, data-ink, its tokens) and every rule the Torn & Waxed identity forbids (shadows, radii above
// md, weight, coloured icon layers, washes as text). A hit means an unstyled or off-brand element in
// production, silently: Tailwind emits nothing for an unknown class, and Kentico stores class strings as
// content. See docs/brand/kit.md → Tokens.
const RETIRED_TOKEN =
  'bone(?:-dark)?|onyx(?:-light)?|surface-\\d{3}|overlay-\\d{3}|ink-line|ink-on-wash|hatch|edge(?:-strong)?|flap-[12]|link|danger(?:-ink)?|success(?:-ink)?|warning(?:-ink)?|rating-ink|rosewater|flamingo|mauve|maroon|sapphire|blue'
const WASH = 'peach|yellow|green|teal|sky|lavender|pink|red'
const UTILITY =
  '(?:bg|text|border|fa-primary|fa-secondary|ring|outline|fill|stroke|from|to|via|divide|placeholder|decoration|accent|caret)'
const VARIANTS = '(?:[a-z-]+(?:\\/[a-z0-9-]+)?:)*'
const EDGE = ['(?<![\\w-])', '(?![\\w-])'] as const

const RETIRED = new RegExp(
  [
    `${EDGE[0]}${VARIANTS}${UTILITY}-(?:${RETIRED_TOKEN})${EDGE[1]}`, // retired colour tokens as utilities
    `${EDGE[0]}${VARIANTS}text-(?:${WASH})${EDGE[1]}`, // washes are fills, never text
    `${EDGE[0]}${VARIANTS}shadow-[a-z0-9-]+${EDGE[1]}`, // every shadow utility
    `${EDGE[0]}${VARIANTS}rounded(?:-[trbl]{1,2})?-(?:lg|xl|2xl|3xl|4xl)${EDGE[1]}`, // radii above md
    `${EDGE[0]}${VARIANTS}font-(?:bold|semibold|medium)${EDGE[1]}`, // one weight of everything
    `${EDGE[0]}${VARIANTS}fa-(?:primary|secondary)-[a-z0-9-]+${EDGE[1]}`, // coloured icon layers
    `${EDGE[0]}sk-[a-z][a-z0-9-]*`, // the Softbound kit
    '\\bv-ink\\b|\\bdata-ink\\b', // the drawn outline
    'font-weight:\\s*(?:[5-9]00|bold(?:er)?)', // weight in CSS
    `var\\(--color-(?:${RETIRED_TOKEN})\\)`, // retired tokens read from CSS
  ].join('|'),
)

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const ROOTS = ['Features', 'App_Data/CIRepository'].map((dir) => join(WEB, dir))
const SCAN = /\.(vue|cshtml|ts|cs|css|xml|json)$/

// Unconverted Softbound surfaces, relative to src/KCC.Web with forward slashes; a trailing slash allows a
// directory. Each conversion task deletes its entry. An entry that no longer hits fails the second test, so
// this list can only shrink; it is empty by the cleanup phase.
const ALLOWLIST = new Set<string>([
  'Features/Components/Badge/Badge.vue',
  'Features/Components/Breadcrumbs/Breadcrumbs.Component.vue',
  'Features/Components/Button/Button.vue',
  'Features/Components/ComingSoon/ComingSoonSection.vue',
  'Features/Components/Forms/Field.vue',
  'Features/Components/Forms/InputField.vue',
  'Features/Components/Forms/NumberStepper.vue',
  'Features/Components/Forms/RangeSlider.vue',
  'Features/Components/Forms/TextAreaField.vue',
  'Features/Components/Header/MenuItem.vue',
  'Features/Components/Recipe/AccentTile.vue',
  'Features/Components/Recipe/DetailHero.vue',
  'Features/Components/Recipe/FeaturedRecipeCard.vue',
  'Features/Components/Recipe/RecipeCard.vue',
  'Features/Components/Recipe/RecipeCardRow.vue',
  'Features/Components/Recipe/SegmentedControl.vue',
  'Features/Components/Recipe/StatTiles.vue',
  'Features/Components/RecipeDetail/VariantGrid.vue',
  'Features/Components/RecipeDetail/VariantToolbar.vue',
  'Features/Components/RecipeDetail/VariantsEmptyState.vue',
  'Features/Components/RecipeSearch/AppliedFilterChips.vue',
  'Features/Components/RecipeSearch/RecipeFilters.vue',
  'Features/Components/RecipeSearch/RecipeResultsToolbar.vue',
  'Features/Components/RecipeSearch/RecipeSearchHeader.vue',
  'Features/Components/RecipeSearch/RecipesEmptyState.vue',
  'Features/Components/Sketch/Sheet.vue',
  'Features/Components/StarRating/RatingSummary.vue',
  'Features/Components/StarRating/StarRating.vue',
  'Features/Components/Theme/ThemeToggle.vue',
  'Features/Components/VariantDetail/VariantCookNotes.vue',
  'Features/Components/VariantDetail/VariantCookedToggle.vue',
  'Features/Components/VariantDetail/VariantIngredients.vue',
  'Features/Components/VariantDetail/VariantInstructions.vue',
  'Features/Components/VariantDetail/VariantNutrition.vue',
  'Features/Components/VariantDetail/VariantReviews.vue',
  'Features/Components/VariantDetail/VariantSiblings.vue',
  'Features/Ink/inkDom.ts',
  'Features/Ink/vInk.ts',
  'Features/Main.ts',
  'Features/PageBuilderMount.ts',
  'Features/Pages/Account/AccountView.Component.vue',
  'Features/Pages/Account/Login/LoginView.Component.vue',
  'Features/Pages/Account/RegistrationComplete/Index.cshtml',
  'Features/Pages/Account/Settings/AccountSettingsView.Component.vue',
  'Features/Pages/AddVariant/AddVariantView.Component.vue',
  'Features/Pages/CreateRecipe/CreateRecipeView.Component.vue',
  'Features/Pages/Error/Index.cshtml',
  'Features/Pages/RecipeDetail/RecipeDetailView.Component.vue',
  'Features/Pages/RecipeSearch/RecipeSearchView.Component.vue',
  'Features/Pages/VariantDetail/CookMode.vue',
  'Features/Pages/VariantDetail/CookModeStep.vue',
  'Features/Pages/VariantDetail/StepTimer.vue',
  'Features/Pages/VariantDetail/VariantDetailView.Component.vue',
  'Features/Sections/Base/BaseSection.cshtml',
  'Features/Sections/Base/SectionColors.cs',
  'Features/Sections/MultipleColumn/MultipleColumnSection.cshtml',
  'Features/Styles/Sketch/',
  'Features/TagHelpers/ButtonLinkTagHelper.cs',
  'Features/Types/DesignSystem.ts',
  'Features/Widgets/Button/ButtonWidget.cshtml',
  'Features/Widgets/Card/Card.Component.vue',
  'Features/Widgets/Card/CardWidget.cshtml',
  'Features/Widgets/Card/Grid/CardGridWidget.cshtml',
  'Features/Widgets/Hero/SmallHero.Component.vue',
  'Features/Widgets/Hero/SmallHeroWidget.cshtml',
  'Features/Widgets/Image/ImageWidget.cshtml',
  'Features/Widgets/Stacker/Stacker.Component.vue',
  'Features/Widgets/Stacker/StackerWidget.cshtml',
  'App_Data/CIRepository/@global/cms.contenttype/kcc.carditem.xml',
  'App_Data/CIRepository/@global/contentitemdata.kcc.carditem/',
])

const covers = (entry: string, path: string) => (entry.endsWith('/') ? path.startsWith(entry) : path === entry)

function* files(dir: string): Generator<string> {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) yield* files(path)
    else if (SCAN.test(entry.name)) yield path
  }
}

// `font-weight` inside an @font-face block is a descriptor naming a face (Hazelnut Bold stays declared for
// content that still carries <strong>), not weight applied to text. Blank those blocks out line by line so
// line numbers in the report stay right and the weight rule only sees applied weight.
const withoutFontFaces = (css: string) => css.replace(/@font-face\s*\{[^}]*\}/g, (block) => block.replace(/[^\n]/g, ' '))

const hits = new Map<string, string[]>()
for (const root of ROOTS) {
  for (const file of files(root)) {
    const path = relative(WEB, file).replaceAll('\\', '/')
    const source = readFileSync(file, 'utf8')
    const lines = (file.endsWith('.css') ? withoutFontFaces(source) : source)
      .split('\n')
      .flatMap((line, i) => (RETIRED.test(line) ? [`${i + 1}: ${line.trim().slice(0, 120)}`] : []))
    if (lines.length) hits.set(path, lines)
  }
}

describe('retired design tokens', () => {
  it('appear nowhere in converted source or persisted CMS content', () => {
    const offending = [...hits]
      .filter(([path]) => ![...ALLOWLIST].some((entry) => covers(entry, path)))
      .flatMap(([path, lines]) => lines.map((line) => `${path}:${line}`))
    expect(offending).toEqual([])
  })

  it('the allowlist only names files that still need it', () => {
    const stale = [...ALLOWLIST].filter((entry) => ![...hits.keys()].some((path) => covers(entry, path)))
    expect(stale).toEqual([])
  })
})
