import { defineComponent, h, type Component } from 'vue'
import type { WizardPhoto } from '~/Components/RecipeWizard/recipeWizardFlow'
import { recipeWizardKeys } from '~/Components/RecipeWizard/recipeWizardKeys'
import type { Mark } from '~/Components/RecipeWizard/wizardValues'
import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
import { renderSsr, tagWith } from './ssr'

// Every key reads «Key», so a string left unresolved shows up as RecipeWizard.Key.
export function wizardStrings(english: Record<string, string> = {}): Record<string, string> {
  const texts = { ...Object.fromEntries(Object.keys(recipeWizardKeys).map((key) => [key, `«${key}»`])), ...english }
  return Object.fromEntries(Object.entries(texts).map(([key, text]) => [`RecipeWizard.${key}`, text]))
}

export const renderInWizard = (
  component: Component,
  props: Record<string, unknown> = {},
  english?: Record<string, string>,
): Promise<string> =>
  renderSsr(
    defineComponent({
      setup() {
        provideResourceStrings(wizardStrings(english), 'RecipeWizard')
        return () => h(component, props)
      },
    }),
    undefined,
    undefined,
    { ResourceString },
  )

export const unresolvedKeys = (html: string) => html.match(/\bRecipeWizard\.\w+/g) ?? []

export const wizardPhoto = (n: number): WizardPhoto => ({
  id: `photo-${n}`,
  blob: new Blob([`page ${n}`], { type: 'image/jpeg' }),
  url: `blob:https://kitchen.test/photo-${n}`,
  source: 'device',
})

// Buttons cannot nest, so the first close after a button's opening tag is its own.
export const buttonsWith = (html: string, needle: string) =>
  html.match(new RegExp(`<button[^>]*${needle}[^>]*>[\\s\\S]*?</button>`, 'g')) ?? []

export const buttonWith = (html: string, needle: string) => buttonsWith(html, needle)[0] ?? ''

export const buttonHolding = (html: string, text: string) =>
  buttonsWith(html, '').find((button) => button.includes(text)) ?? ''

export const classesOf = (tag: string) => tag.match(/\sclass="([^"]*)"/)?.[1]?.split(/\s+/) ?? []

export const markerButtons = (html: string) =>
  buttonsWith(html, '').filter((button) => {
    const classes = classesOf(button.match(/^<button[^>]*>/)?.[0] ?? '')
    return classes.includes('kcc-btn') && !classes.some((name) => /^kcc-btn--(ghost|text|ink)$/.test(name))
  })

export const readerMark = (kind: Mark['kind'], reason: string): Mark => ({ kind, reason, code: null, arg: null })

export const markKinds = (html: string) =>
  (html.match(/<[a-z]+[^>]*data-testid="value-mark"[^>]*>/g) ?? []).map((tag) => tag.match(/data-kind="(\w+)"/)?.[1])

export const describedBy = (html: string, controlNeedle: string) => {
  const id = tagWith(html, controlNeedle).match(/\saria-describedby="([^"]+)"/)?.[1]
  return id ? tagWith(html, `\\sid="${id}"`) : ''
}

const VOID = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr'])

export const enclosingTag = (html: string, needle: string, outward = 0) => {
  const found = html.search(new RegExp(needle))
  if (found < 0) throw new Error(`${needle} is not in the markup`)
  const at = html.lastIndexOf('<', found)
  const open: { name: string; tag: string }[] = []
  for (const [tag, closing, name = ''] of html.slice(0, at).matchAll(/<!--[\s\S]*?-->|<(\/?)([a-z0-9]+)\b[^>]*>/g)) {
    if (!name) continue
    if (closing) {
      const index = open.findLastIndex((element) => element.name === name)
      if (index < 0) throw new Error(`</${name}> closes no open tag before ${needle}`)
      open.splice(index)
    } else if (!VOID.has(name) && !tag.endsWith('/>')) open.push({ name, tag })
  }
  const tag = open.at(-1 - outward)?.tag
  if (!tag) throw new Error(`no element ${outward} out from ${needle}`)
  return tag
}
