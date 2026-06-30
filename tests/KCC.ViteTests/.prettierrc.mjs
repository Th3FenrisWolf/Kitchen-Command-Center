// Prettier config for the Vite test sources, which live here under /tests/KCC.ViteTests
// rather than inside src/KCC.Web (see tsconfig.json in this folder for the same arrangement
// and the reasoning). Prettier applies the *nearest* config walking up from each file, and
// this tree is a sibling of src/KCC.Web — so without a config here these files fall back to
// Prettier's defaults (double quotes, semicolons) instead of the project style.
//
// Rather than duplicate the option values, re-derive them from src/KCC.Web/.prettierrc as the
// single source of truth. The Tailwind / CSS-order plugin entries are dropped: these tests are
// pure TypeScript so those plugins are no-ops here, and their bare specifiers wouldn't resolve
// from this tree anyway (node_modules lives under src/KCC.Web). $schema is a JSON editor hint
// with no meaning in a JS config, so it is dropped too.
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'

const base = JSON.parse(readFileSync(fileURLToPath(new URL('../../src/KCC.Web/.prettierrc', import.meta.url)), 'utf8'))

const { $schema, plugins, tailwindStylesheet, cssDeclarationSorterKeepOverrides, cssDeclarationSorterOrder, ...core } = base

export default core
