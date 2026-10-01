// Prettier applies the nearest config, and these tests sit outside src/KCC.Web, so this one reuses that project's
// options. The plugin entries are left out: the tests are plain TypeScript, and the plugins would not resolve here.
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const base = JSON.parse(readFileSync(fileURLToPath(new URL('../../src/KCC.Web/.prettierrc', import.meta.url)), 'utf8'))

const { $schema, plugins, tailwindStylesheet, cssDeclarationSorterKeepOverrides, cssDeclarationSorterOrder, ...core } = base

export default core
