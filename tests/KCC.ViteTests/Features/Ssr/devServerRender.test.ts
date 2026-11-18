import { createRequire } from 'node:module'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { afterAll, beforeAll, describe, expect, it } from 'vitest'
import { createServer, type ViteDevServer } from 'vite'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const requireFromWeb = createRequire(join(WEB, 'package.json'))

const payload = {
  headerContent: '<header>top</header>',
  bodyContent: '<p>body</p>',
  footerContent: '<footer>end</footer>',
  isPreview: false,
  bloom: { seed: 2671381617, page: '/recipes/brown-butter-gnocchi' },
}

// Vite resolves a deduped package from the project root alone, and leaves an import it cannot resolve there inlined.
// Under pnpm's strict layout that inlined Vue's CommonJS build into the module runner, which cannot evaluate it.
describe('the dev SSR server', () => {
  let vite: ViteDevServer

  beforeAll(async () => {
    vite = await createServer({
      root: WEB,
      configFile: join(WEB, 'vite.config.ts'),
      logLevel: 'silent',
      server: { middlewareMode: true, hmr: false },
      appType: 'custom',
    })
  }, 60_000)

  afterAll(async () => {
    await vite?.close()
  })

  it('loads the SSR entry and renders the app as Server.js does', async () => {
    const { createApp } = await vite.ssrLoadModule(join(WEB, 'Features/Ssr/Server.Entry.ts'))
    const { renderToString } = requireFromWeb('vue/server-renderer')

    const html = await renderToString(createApp(payload).app)

    expect(html).toContain('<p>body</p>')
    expect(html).toContain('<footer>end</footer>')
  }, 60_000)

  // Two Vue runtimes render without an error, so only the module cache shows the entry and the renderer share one.
  it('renders with the one Vue runtime vue/server-renderer loads', async () => {
    const { createApp } = await vite.ssrLoadModule(join(WEB, 'Features/Ssr/Server.Entry.ts'))
    const { renderToString } = requireFromWeb('vue/server-renderer')

    await renderToString(createApp(payload).app)

    const runtimes = Object.keys(requireFromWeb.cache).filter((path) => /runtime-core\.cjs(\.prod)?\.js$/.test(path))
    expect(runtimes).toHaveLength(1)
  }, 60_000)
})
