import { readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')

const SHEET_TAG = /^kcc-?(?:sheet|washes)$/i
const WASHES_TAG = /^kcc-?washes$/i
const VOID = new Set(['area', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr'])

// The pad's `.pad-page` and every blank are paper under a top sheet; wells, tiles, labels and tape sit on one.
// None of them blooms.
const BARE = /(?<![\w-])(?:kcc-blank|pad-page|kcc-well|kcc-tile|kcc-tilewrap|kcc-label|kcc-tape)(?![\w])/

const TOKEN =
  /<!--[\s\S]*?-->|<\/([A-Za-z][\w.:-]*)\s*>|<([A-Za-z][\w.:-]*)((?:\s+[^\s=/>"']+(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>"']+))?)*)\s*(\/?)>/g
const ATTRIBUTE = /([^\s=/>"']+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>"']+)))?/g

interface Element {
  name: string
  attributes: Map<string, string>
  line: number
}

const attributesOf = (source: string) =>
  new Map([...source.matchAll(ATTRIBUTE)].map(([, name, ...values]) => [name!, values.find((v) => v !== undefined) ?? '']))

// Every alias after the first in a `v-for`: an array's index, or an object's key and index.
const indexAliases = (element: Element) => {
  const loop = element.attributes.get('v-for')?.match(/^\s*\(([^)]*)\)\s+(?:in|of)\s/)
  return loop
    ? loop[1]!
        .split(',')
        .slice(1)
        .map((alias) => alias.trim())
        .filter(Boolean)
    : []
}

const razorCounters = (source: string) => [
  ...[...source.matchAll(/\bfor\s*\(\s*(?:var|int)\s+(\w+)/g)].map(([, counter]) => counter!),
  ...[...source.matchAll(/\.Select\(\s*\(\s*\w+\s*,\s*(\w+)\s*\)\s*=>/g)].map(([, index]) => index!),
]

const reads = (expression: string, name: string) => new RegExp(`(?<![\\w.$])${name}(?![\\w$])`).test(expression)

const templateOf = (path: string, source: string) => {
  if (!path.endsWith('.vue')) return { text: source, offset: 0 }
  const start = source.indexOf('<template')
  const end = source.lastIndexOf('</template>')
  return start < 0 ? { text: '', offset: 0 } : { text: source.slice(start, end), offset: start }
}

const keyProblems = (path: string, source: string): string[] => {
  const { text, offset } = templateOf(path, source)
  const lineAt = (index: number) => source.slice(0, offset + index).split('\n').length
  const counters = path.endsWith('.cshtml') ? razorCounters(source) : []
  const open: Element[] = []
  const problems: string[] = []

  for (const token of text.matchAll(TOKEN)) {
    const [, closing, name, attributeSource, selfClosing] = token
    if (closing) {
      const at = open.findLastIndex((element) => element.name.toLowerCase() === closing.toLowerCase())
      if (at >= 0) open.length = at
      continue
    }
    if (!name) continue

    const element: Element = { name, attributes: attributesOf(attributeSource ?? ''), line: lineAt(token.index) }
    const where = `${path}:${element.line} <${name}>`

    if (SHEET_TAG.test(name)) {
      const bound = ['wash-key', ':wash-key', 'v-bind:wash-key'].find((key) => element.attributes.has(key))
      if (!bound) problems.push(`${where} has no wash-key`)
      else {
        const value = element.attributes.get(bound)!
        const isExpression = bound !== 'wash-key' || value.includes('@')
        const aliases = [...open, element].flatMap(indexAliases).concat(counters)
        const index = isExpression ? aliases.find((alias) => reads(value, alias)) : undefined
        if (index) problems.push(`${where} keys on the loop index \`${index}\``)
      }

      if (WASHES_TAG.test(name)) {
        const host = open.find((ancestor) =>
          ['class', ':class'].some((key) => BARE.test(ancestor.attributes.get(key) ?? '')),
        )
        if (host) problems.push(`${where} sits inside <${host.name}> at line ${host.line}, which never blooms`)
      }
    }

    if (!selfClosing && !VOID.has(name.toLowerCase())) open.push(element)
  }

  return problems
}

describe('the wash-key scan', () => {
  it('flags a sheet with no key', () => {
    expect(keyProblems('A.vue', '<template><KccSheet :tear="2">x</KccSheet></template>')).toEqual([
      'A.vue:1 <KccSheet> has no wash-key',
    ])
  })

  it('passes a fixed key, a bound key and a list item keyed by its own id', () => {
    const source = `<template>
  <KccSheet wash-key="intro" />
  <KccSheet :wash-key="key" />
  <ul><KccSheet v-for="(item, i) in items" :key="item.id" :wash-key="\`item:\${item.id}\`" /></ul>
</template>`
    expect(keyProblems('A.vue', source)).toEqual([])
  })

  it('flags a key read from the index of the sheet’s own or an enclosing v-for', () => {
    const source = `<template>
  <KccSheet v-for="(item, i) in items" :wash-key="\`item:\${i}\`" />
  <li v-for="(group, at) in groups" :key="group.id">
    <KccSheet :wash-key="'group-' + at" />
  </li>
</template>`
    expect(keyProblems('A.vue', source)).toEqual([
      'A.vue:2 <KccSheet> keys on the loop index `i`',
      'A.vue:4 <KccSheet> keys on the loop index `at`',
    ])
  })

  it('flags a Razor loop counter in a key', () => {
    const source = `@for (var n = 0; n < Model.Count; n++)
{
  <KccWashes wash-key="row:@n" />
}`
    expect(keyProblems('A.cshtml', source)).toEqual(['A.cshtml:3 <KccWashes> keys on the loop index `n`'])
  })

  it('flags a field inside paper that never blooms', () => {
    const source = `<template>
  <span class="kcc-blank" aria-hidden="true"><KccWashes wash-key="blank" /></span>
  <div class="kcc-tilewrap"><KccWashes wash-key="tile" /></div>
  <div class="kcc-sheet"><KccWashes wash-key="sheet" /></div>
</template>`
    expect(keyProblems('A.vue', source)).toEqual([
      'A.vue:2 <KccWashes> sits inside <span> at line 2, which never blooms',
      'A.vue:3 <KccWashes> sits inside <div> at line 3, which never blooms',
    ])
  })
})

describe('every sheet in Features', () => {
  const problems = [...files(FEATURES, /\.(vue|cshtml)$/)].flatMap((file) =>
    keyProblems(relative(WEB, file).replaceAll('\\', '/'), readFileSync(file, 'utf8')),
  )

  it('carries a stable wash-key, never a loop index, and blooms only on a top sheet', () => {
    expect(problems).toEqual([])
  })
})
