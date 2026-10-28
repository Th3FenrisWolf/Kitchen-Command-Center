import postcss, { type Container, type Rule } from 'postcss'
import { keptIcons, keptUtilities } from './iconSafelist'

export const fontAwesomeUtilities: readonly string[] = `
  1x 2x 3x 4x 5x 6x 7x 8x 9x 10x 2xs xs sm lg xl 2xl width-auto fw width-fixed canvas-square canvas-roomy ul li border
  pull-left pull-start pull-right pull-end beat bounce fade beat-fade flip flip-360 shake spin spin-reverse pulse
  spin-pulse spin-snap spin-snap-4 spin-snap-8 buzz wag float swing jello rotate-90 rotate-180 rotate-270
  flip-horizontal flip-vertical flip-both rotate-by stack stack-1x stack-2x inverse swap-opacity
`
  .split(/\s+/)
  .filter(Boolean)

const utilities = new Set(fontAwesomeUtilities)
const unusedUtility = (name: string) => utilities.has(name) && !keptUtilities.has(name)

const classesOf = (selector: string) => [...selector.matchAll(/\.fa-([A-Za-z0-9-]+)/g)].map((match) => match[1]!)

const isIconRule = (rule: Rule) => rule.some((node) => node.type === 'decl' && node.prop === '--fa')

const keeps = (rule: Rule) => {
  if (isIconRule(rule)) return (selector: string) => classesOf(selector).every((name) => keptIcons.has(name))
  return (selector: string) => !classesOf(selector).some(unusedUtility)
}

function removeIfEmpty(container: Container | undefined) {
  if (container?.type !== 'atrule' || container.nodes?.length !== 0) return
  const parent = container.parent as Container | undefined
  container.remove()
  removeIfEmpty(parent)
}

export function subsetFontAwesome(css: string): string {
  const root = postcss.parse(css)
  const emptied = new Set<Container>()
  const remove = (node: Rule | postcss.AtRule) => {
    if (node.parent) emptied.add(node.parent as Container)
    node.remove()
  }

  root.walkAtRules(/keyframes$/, (keyframes) => {
    if (keyframes.params.startsWith('fa-') && unusedUtility(keyframes.params.slice(3))) remove(keyframes)
  })

  root.walkRules((rule) => {
    const kept = rule.selectors.filter(keeps(rule))
    if (kept.length === 0) remove(rule)
    else if (kept.length < rule.selectors.length) rule.selectors = kept
  })

  emptied.forEach(removeIfEmpty)

  return root.toString()
}
