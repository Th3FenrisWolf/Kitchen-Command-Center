import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import postcss from 'postcss'
import { describe, expect, it } from 'vitest'
import { keptIcons, keptUtilities } from '~/Styles/FontAwesome/iconSafelist'
import { fontAwesomeUtilities, subsetFontAwesome } from '~/Styles/FontAwesome/subsetFontAwesome'
import { files } from '../../support/files'

const SRC = fileURLToPath(new URL('../../../../src/', import.meta.url))
const WEB = join(SRC, 'KCC.Web')
const require = createRequire(import.meta.url)

const fontAwesomeCss = ['fontawesome', 'solid', 'regular', 'duotone']
  .map((style) => readFileSync(require.resolve(`@fortawesome/fontawesome-pro/css/${style}.css`), 'utf8'))
  .join('\n')

const FA_CLASS = /\.fa-([A-Za-z0-9-]+)/g
const classesOf = (selector: string) => [...selector.matchAll(FA_CLASS)].map((match) => match[1]!)

const iconsIn = (css: string) => {
  const icons = new Set<string>()
  postcss.parse(css).walkRules((rule) => {
    if (rule.some((node) => node.type === 'decl' && node.prop === '--fa'))
      classesOf(rule.selector).forEach((name) => icons.add(name))
  })
  return icons
}

const fontAwesomeIcons = iconsIn(fontAwesomeCss)
const utilities = new Set(fontAwesomeUtilities)

const sources = [
  ...files(join(WEB, 'Features'), /\.(vue|cshtml|ts|cs)$/),
  ...files(join(WEB, 'Views'), /\.cshtml$/),
  ...files(join(WEB, 'uSync'), /\.config$/),
  ...files(join(SRC, 'KCC.Admin'), /\.cs$/),
].filter((path) => !/[\\/](bin|obj|node_modules)[\\/]/.test(path))

const referenced = (universe: ReadonlySet<string>) => {
  const found = new Map<string, string>()
  for (const path of sources) {
    for (const match of readFileSync(path, 'utf8').matchAll(/(?<![\w-])fa-([a-z0-9-]+)/g)) {
      if (universe.has(match[1]!) && !found.has(match[1]!)) found.set(match[1]!, relative(SRC, path))
    }
  }
  return found
}

const recipeIcons = readFileSync(join(SRC, 'KCC.Admin/RecipeIcons.cs'), 'utf8')
  .match(/All = """([\s\S]*?)"""/)![1]!
  .split(/\s+/)
  .filter(Boolean)

const taxonomyIcons = [
  ...readFileSync(join(WEB, 'Features/Recipes/TaxonomyDefaults.cs'), 'utf8').matchAll(/"fa-duotone fa-([a-z0-9-]+)"/g),
].map((match) => match[1]!)

const subset = subsetFontAwesome(fontAwesomeCss)

describe('Font Awesome safety list', () => {
  it('keeps every icon the sources name', () => {
    const missing = [...referenced(fontAwesomeIcons)].filter(([name]) => !keptIcons.has(name))
    expect(missing.map(([name, path]) => `fa-${name} (${path})`)).toEqual([])
  })

  it('keeps every recipe icon the backoffice picker offers', () => {
    expect(recipeIcons.length).toBeGreaterThan(100)
    expect(recipeIcons.filter((name) => !keptIcons.has(name))).toEqual([])
  })

  it('keeps every taxonomy default icon', () => {
    expect(taxonomyIcons.length).toBeGreaterThan(0)
    expect(taxonomyIcons.filter((name) => !keptIcons.has(name))).toEqual([])
  })

  it('names only real Font Awesome icons', () => {
    expect([...keptIcons].filter((name) => !fontAwesomeIcons.has(name))).toEqual([])
  })

  it('keeps every Font Awesome utility the sources use', () => {
    const missing = [...referenced(utilities)].filter(([name]) => !keptUtilities.has(name))
    expect(missing.map(([name, path]) => `fa-${name} (${path})`)).toEqual([])
  })

  it('knows every Font Awesome utility the stylesheet defines', () => {
    const root = postcss.parse(fontAwesomeCss)
    let styles = new Set<string>()
    root.walkRules((rule) => {
      styles = new Set(classesOf(rule.selector))
      return false
    })
    const defined = new Set<string>()
    root.walkRules((rule) => {
      if (rule.some((node) => node.type === 'decl' && node.prop === '--fa')) return
      classesOf(rule.selector)
        .filter((name) => !styles.has(name))
        .forEach((name) => defined.add(name))
    })
    expect([...defined].sort()).toEqual([...utilities].sort())
  })
})

describe('Font Awesome subset', () => {
  it('ships an icon rule for exactly the kept icons', () => {
    expect([...iconsIn(subset)].sort()).toEqual([...keptIcons].sort())
  })

  it('drops every utility rule and keyframe the site does not use', () => {
    const shipped = new Set<string>()
    const root = postcss.parse(subset)
    root.walkRules((rule) => classesOf(rule.selector).forEach((name) => utilities.has(name) && shipped.add(name)))
    root.walkAtRules('keyframes', (keyframes) => {
      shipped.add(keyframes.params.replace(/^fa-/, ''))
    })
    expect([...shipped].sort()).toEqual([...keptUtilities].sort())
  })

  it('keeps the base, style and font rules', () => {
    const root = postcss.parse(subset)
    const selectors: string[] = []
    root.walkRules((rule) => {
      selectors.push(...rule.selectors.map((selector) => selector.replace(/\s+/g, ' ')))
    })
    expect(selectors).toEqual(
      expect.arrayContaining(['.fa', '.fa-solid', '.fa-regular', '.fa-duotone', '.fa-duotone::after']),
    )
    let fontFaces = 0
    root.walkAtRules('font-face', () => {
      fontFaces++
    })
    expect(fontFaces).toBe(3)
  })

  it('leaves rules outside Font Awesome alone', () => {
    const own = '.kcc-sheet .fa-duotone { --fa-secondary-opacity: 1; }\n.fa-stack-note { color: red; }'
    expect(subsetFontAwesome(own)).toBe(own)
  })
})

describe('Font Awesome subset wiring', () => {
  it('runs on the main stylesheet in the Vite config', () => {
    const config = readFileSync(join(WEB, 'vite.config.ts'), 'utf8')
    expect(config).toContain('subsetFontAwesome')
    expect(config).toContain('fontAwesomeSubsetPlugin()')
  })
})
