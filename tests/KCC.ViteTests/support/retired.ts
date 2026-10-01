// Retired outright, no alias layer: the pre-sketch tokens (bone / onyx / surface / overlay), the Softbound kit
// (sk-*, v-ink, data-ink, its tokens) and every rule the Torn & Waxed identity forbids (shadows, radii above
// md, weight, coloured icon layers, washes as text). A hit means an unstyled or off-brand element in
// production, silently: Tailwind emits nothing for an unknown class, and CMS content, which uSync exports into
// the repo, carries class strings too. See docs/brand/kit.md → Tokens.
const RETIRED_TOKEN =
  'bone(?:-dark)?|onyx(?:-light)?|surface-\\d{3}|overlay-\\d{3}|ink-line|ink-on-wash|hatch|edge(?:-strong)?|flap-[12]|link|danger(?:-ink)?|success(?:-ink)?|warning(?:-ink)?|rating-ink|rosewater|flamingo|mauve|maroon|sapphire|blue'
const WASH = 'peach|yellow|green|teal|sky|lavender|pink|red'
const UTILITY =
  '(?:bg|text|border|fa-primary|fa-secondary|ring|outline|fill|stroke|from|to|via|divide|placeholder|decoration|accent|caret)'
const VARIANTS = '(?:[a-z-]+(?:\\/[a-z0-9-]+)?:)*'
const EDGE = ['(?<![\\w-])', '(?![\\w-])'] as const

export const RETIRED = new RegExp(
  [
    `${EDGE[0]}${VARIANTS}${UTILITY}-(?:${RETIRED_TOKEN})${EDGE[1]}`, // retired colour tokens as utilities
    `${EDGE[0]}${VARIANTS}text-(?:${WASH})${EDGE[1]}`, // washes are fills, never text
    // every shadow utility, including drop-/inset-/text- families and arbitrary or variable values
    `${EDGE[0]}${VARIANTS}(?:drop-|inset-|text-)?shadow-(?:\\[[^\\]]*\\]|\\([^)]*\\)|[a-z0-9-]+)${EDGE[1]}`,
    `${EDGE[0]}${VARIANTS}rounded(?:-[trbl]{1,2})?-(?:lg|xl|2xl|3xl|4xl)${EDGE[1]}`, // radii above md
    `${EDGE[0]}${VARIANTS}font-(?:bold|semibold|medium|\\[[^\\]]*\\])${EDGE[1]}`, // one weight of everything
    `${EDGE[0]}${VARIANTS}fa-(?:primary|secondary)-[a-z0-9-]+${EDGE[1]}`, // coloured icon layers
    `${EDGE[0]}sk-[a-z][a-z0-9-]*`, // the Softbound kit
    '\\bv-ink\\b|\\bdata-ink\\b', // the drawn outline
    'font-weight:\\s*(?:[5-9]00|bold(?:er)?)', // weight in CSS
    `var\\(--color-(?:${RETIRED_TOKEN})\\)`, // retired tokens read from CSS
  ].join('|'),
)
