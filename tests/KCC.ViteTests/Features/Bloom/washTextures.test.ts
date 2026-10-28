import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import type { UserConfig } from 'vite'
import { describe, expect, it } from 'vitest'
import { BLOB_COUNT, bakeWashes, washTextures } from '~/Bloom/bakeWashes'
import { WASH_TEXTURE_FOLDER, inlineUnlessWashTexture } from '~/Bloom/washTextures'
import viteConfig from '../../../../src/KCC.Web/vite.config'

const web = (path: string) => fileURLToPath(new URL(`../../../../src/KCC.Web/${path}`, import.meta.url))
const baked = washTextures(bakeWashes()).map(({ file }) => file)
const textures = baked.map((file) => web(`Features/Styles/Torn/${WASH_TEXTURE_FOLDER}/${file}`))
const manifestPath = web('wwwroot/.vite/manifest.json')

describe('the wash textures in the Vite build', () => {
  it('names a grain and one file per blob', () => {
    expect(textures).toHaveLength(1 + BLOB_COUNT)
  })

  it.each(textures)('never inlines %s, so it is fetched and cached apart from the stylesheet', (file) => {
    expect(inlineUnlessWashTexture(file)).toBe(false)
  })

  it('leaves every other asset to Vite’s default', () => {
    for (const file of [
      web('Features/Styles/fonts/Sono-Regular-latin.woff2'),
      web('Features/Images/wash-grain.png'),
      web(`Features/Styles/Torn/${WASH_TEXTURE_FOLDER}/other.png`),
      web('Features/Styles/Torn/wash-blob-0.png'),
    ])
      expect(inlineUnlessWashTexture(file)).toBeUndefined()
  })

  it('is the client build’s inline limit', async () => {
    const factory = viteConfig as unknown as (env: {
      command: 'build'
      mode: string
      isSsrBuild: boolean
      isPreview: boolean
    }) => UserConfig
    const config = factory({ command: 'build', mode: 'production', isSsrBuild: false, isPreview: false })
    expect(config.build?.assetsInlineLimit).toBe(inlineUnlessWashTexture)
  })
})

describe('the wash texture preloads in Layout.cshtml', () => {
  const layout = readFileSync(web('Features/Pages/Shared/Layout.cshtml'), 'utf8')
  const block = layout.slice(layout.indexOf('washTextures'), layout.indexOf('</head>'))

  it('preloads them as CORS images, so the mask fetch reuses the preload', () => {
    expect(block).toMatch(/<link rel="preload" as="image" type="image\/png" crossorigin href="~\/@\w+" \/>/)
  })

  it('reads them from the build manifest outside Development, as the fonts are', () => {
    expect(layout).toMatch(
      /@if \(!Env\.IsDevelopment\(\) && ViteManifest\["Features\/Styles\/Main\.css"\] is \{ Assets: var washTextures \}/,
    )
  })
})

describe.runIf(existsSync(manifestPath))('the wash textures in the built manifest', () => {
  it('lists every texture under Main.css, where Layout.cshtml’s preload filter finds them', () => {
    const manifest = JSON.parse(readFileSync(manifestPath, 'utf8')) as Record<string, { assets?: string[] }>
    const preloaded = (manifest['Features/Styles/Main.css']?.assets ?? [])
      .filter((asset) => asset.startsWith('assets/wash-') && asset.endsWith('.png'))
      .map((asset) => asset.replace(/^assets\//, '').replace(/-[\w-]{8}\.png$/, '.png'))
    expect(preloaded.sort()).toEqual([...baked].sort())
  })
})
