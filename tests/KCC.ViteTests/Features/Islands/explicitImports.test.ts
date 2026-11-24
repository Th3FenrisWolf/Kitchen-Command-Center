import { readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')

const BUILT_INS = new Set([
  'Component',
  'Transition',
  'TransitionGroup',
  'KeepAlive',
  'Teleport',
  'Suspense',
  'Slot',
  'Template',
])

const pascal = (tag: string) => tag.replace(/(?:^|-)([a-z0-9])/g, (_, letter: string) => letter.toUpperCase())

const templateOf = (source: string) => {
  const start = source.indexOf('<template')
  const end = source.lastIndexOf('</template>')
  return start < 0 || end < 0 ? '' : source.slice(start, end).replace(/<!--[\s\S]*?-->/g, '')
}

const componentTags = (template: string) =>
  new Set(
    [...template.matchAll(/<([A-Za-z][\w-]*)/g)]
      .map(([, tag]) => tag!)
      .filter((tag) => /^[A-Z]/.test(tag) || tag.includes('-'))
      .map(pascal)
      .filter((name) => !BUILT_INS.has(name)),
  )

const importedNames = (source: string) => {
  const names = new Set<string>()
  for (const [, clause] of source.matchAll(/\bimport\s+(?!type\b)([^'";]+?)\s+from\s+['"]/g)) {
    const [defaultName, named] = clause!.split(/\{/) as [string, string | undefined]
    const bare = defaultName.replace(/,\s*$/, '').trim()
    if (bare && !bare.startsWith('*')) names.add(bare)
    for (const part of named?.replace(/\}.*/, '').split(',') ?? []) {
      const local = part
        .trim()
        .replace(/^type\s+/, '')
        .split(/\s+as\s+/)
        .pop()
        ?.trim()
      if (local) names.add(local)
    }
  }
  return names
}

const declaredNames = (source: string) =>
  new Set([...source.matchAll(/\b(?:const|let|function)\s+([A-Z]\w*)/g)].map(([, name]) => name!))

describe('component tags in .vue templates', () => {
  it('are each imported or declared by the file that uses them, as the client has no global registry', () => {
    const missing: string[] = []
    for (const path of files(FEATURES, /\.vue$/)) {
      const source = readFileSync(path, 'utf8')
      const imported = new Set([...importedNames(source), ...declaredNames(source)])
      for (const tag of componentTags(templateOf(source)))
        if (!imported.has(tag)) missing.push(`${relative(WEB, path).replaceAll('\\', '/')}: <${tag}>`)
    }
    expect(missing).toEqual([])
  })
})
