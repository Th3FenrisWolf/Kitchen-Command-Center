import { readFileSync } from 'node:fs'
import { builtinModules } from 'node:module'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// The SSR image installs package.json's dependencies and nothing else (the Dockerfile's ssr-deps stage). What the
// server imports at run time must be among them, and what only the build needs must not: Font Awesome Pro alone
// unpacks to almost a gigabyte.
const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const dependencies = Object.keys(JSON.parse(readFileSync(join(WEB, 'package.json'), 'utf8')).dependencies)

const packageOf = (specifier: string) =>
  specifier
    .split('/')
    .slice(0, specifier.startsWith('@') ? 2 : 1)
    .join('/')

const serverImports = ['Features/Ssr/Server.js', 'Features/Ssr/CollectCss.js'].flatMap((file) =>
  [...readFileSync(join(WEB, file), 'utf8').matchAll(/^import\b.*?\bfrom\s+'([^'.][^']*)'/gm)].map(([, specifier]) =>
    packageOf(specifier),
  ),
)

describe('the SSR service image', () => {
  it('installs every package the server imports', () => {
    const packages = serverImports.filter((name) => !builtinModules.includes(name) && !name.startsWith('node:'))

    expect(packages.filter((name) => !dependencies.includes(name))).toEqual([])
  })

  it('leaves packages only the build needs to the build', () => {
    expect(dependencies).not.toContain('@fortawesome/fontawesome-pro')
    expect(dependencies).not.toContain('tailwindcss')
  })
})
