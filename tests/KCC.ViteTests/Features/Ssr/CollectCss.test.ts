import { describe, expect, it, vi } from 'vitest'
import { collectCss, collectCssUrls } from '~/Ssr/CollectCss.js'

const ENTRY = '/app/Features/Ssr/Server.Entry.ts'
const MAIN_CSS = '/app/Features/Styles/Main.css'

interface StubModule {
  url: string
  id: string | null
  file: string | null
  importedModules: Set<StubModule>
}

function mod(url: string, file: string | null = null, imports: StubModule[] = []): StubModule {
  return { url, id: file ?? url, file, importedModules: new Set(imports) }
}

function graphOf(entry: StubModule) {
  return { getModulesByFile: (file: string) => (file === ENTRY ? new Set([entry]) : undefined) }
}

describe('collectCssUrls', () => {
  it('returns CSS modules reachable from the entry, in import order', () => {
    const entry = mod(ENTRY, ENTRY, [
      mod('/Features/App.vue', '/app/Features/App.vue', [
        mod('/Features/App.vue?vue&type=style&index=0&scoped=abc123&lang.css'),
      ]),
      mod('/Features/Components/Recipe/RecipeCard.vue', '/app/Features/Components/Recipe/RecipeCard.vue', [
        mod('/Features/Components/Recipe/RecipeCard.vue?vue&type=style&index=0&scoped=def456&lang.css'),
      ]),
    ])

    expect(collectCssUrls(graphOf(entry), ENTRY)).toEqual([
      '/Features/App.vue?vue&type=style&index=0&scoped=abc123&lang.css',
      '/Features/Components/Recipe/RecipeCard.vue?vue&type=style&index=0&scoped=def456&lang.css',
    ])
  })

  it('prunes a pruned file and everything it imports', () => {
    const entry = mod(ENTRY, ENTRY, [
      mod('/Features/Styles/Main.css', MAIN_CSS, [mod('/node_modules/@fortawesome/css/solid.css', '/fa/solid.css')]),
      mod('/Features/App.vue?vue&type=style&index=0&lang.css'),
    ])

    expect(collectCssUrls(graphOf(entry), ENTRY, [MAIN_CSS])).toEqual(['/Features/App.vue?vue&type=style&index=0&lang.css'])
  })

  it('matches pruned paths written with windows separators', () => {
    const entry = mod(ENTRY, ENTRY, [mod('/Features/Styles/Main.css', MAIN_CSS)])

    expect(collectCssUrls(graphOf(entry), ENTRY, ['\\app\\Features\\Styles\\Main.css'])).toEqual([])
  })

  it('visits each module once when the graph has cycles', () => {
    const a = mod('/Features/a.ts', '/app/Features/a.ts', [mod('/Features/a.css', '/app/Features/a.css')])
    const b = mod('/Features/b.ts', '/app/Features/b.ts', [a])
    a.importedModules.add(b)

    expect(collectCssUrls(graphOf(mod(ENTRY, ENTRY, [a, b])), ENTRY)).toEqual(['/Features/a.css'])
  })

  it('returns nothing when the entry is absent from the graph', () => {
    expect(collectCssUrls(graphOf(mod('/other.ts')), '/app/missing.ts')).toEqual([])
  })
})

describe('collectCss', () => {
  it('appends ?direct to plain CSS urls and &direct to urls that already have a query', async () => {
    const transformRequest = vi.fn().mockResolvedValue({ code: '' })
    const entry = mod(ENTRY, ENTRY, [
      mod('/Features/plain.css', '/app/Features/plain.css'),
      mod('/Features/App.vue?vue&type=style&index=0&lang.css'),
    ])

    await collectCss(graphOf(entry), transformRequest, ENTRY)

    expect(transformRequest.mock.calls.flat()).toEqual([
      '/Features/plain.css?direct',
      '/Features/App.vue?vue&type=style&index=0&lang.css&direct',
    ])
  })

  it('joins compiled CSS and drops modules that transform to nothing', async () => {
    const entry = mod(ENTRY, ENTRY, [
      mod('/Features/a.css', '/app/Features/a.css'),
      mod('/Features/b.css', '/app/Features/b.css'),
      mod('/Features/c.css', '/app/Features/c.css'),
    ])
    const transformRequest = vi
      .fn()
      .mockResolvedValueOnce({ code: '.a{}' })
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce({ code: '.c{}' })

    expect(await collectCss(graphOf(entry), transformRequest, ENTRY)).toBe('.a{}\n.c{}')
  })
})
