// @vitest-environment happy-dom
import { createSSRApp, defineAsyncComponent, defineComponent, h, nextTick } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import VariantReviews from '~/Components/VariantDetail/VariantReviews.vue'
import VariantCookNotes from '~/Components/VariantDetail/VariantCookNotes.vue'
import { get, put, post } from '~/Utilities/Api'

const chunk = vi.hoisted(() => ({ held: false }))

vi.mock('~/Utilities/Api', () => ({ get: vi.fn(), put: vi.fn(), post: vi.fn(), del: vi.fn() }))
vi.mock('~/Components/VariantDetail/memberTextArea', async (importOriginal) => {
  const original = await importOriginal<typeof import('~/Components/VariantDetail/memberTextArea')>()
  const Held = defineAsyncComponent(() => new Promise(() => {}))
  const Arrived = defineAsyncComponent(() => import('~/Components/Forms/TextAreaField.vue'))
  const TextAreaField = defineComponent({ render: () => h(chunk.held ? Held : Arrived) })
  return { ...original, TextAreaField }
})

const GUID = '11111111-2222-3333-4444-555555555555'
const ok = { success: true as const, data: null, errorMessage: null }
const settle = async () => {
  for (let i = 0; i < 4; i++) {
    await new Promise((resolve) => setTimeout(resolve))
    await nextTick()
  }
}

async function hydrate(component: typeof VariantReviews | typeof VariantCookNotes) {
  const props = { variantGuid: GUID, isAuthenticated: true }
  const bloom = createBloom(1, '/p')
  bloom.markHydrated()
  const serverApp = createSSRApp({ render: () => h(component, props) })
  serverApp.provide(BLOOM_KEY, bloom)
  const host = document.createElement('div')
  host.innerHTML = await renderToString(serverApp)
  document.body.append(host)
  chunk.held = true
  const app = createSSRApp({ render: () => h(component, props) })
  app.provide(BLOOM_KEY, bloom)
  return { host, mount: () => app.mount(host) }
}

describe('a submit pressed before the late field has arrived', () => {
  afterEach(() => {
    vi.mocked(get).mockReset()
    vi.mocked(put).mockReset()
    vi.mocked(post).mockReset()
    document.body.innerHTML = ''
    chunk.held = false
  })

  it('sends the review text the reader typed into the server textarea', async () => {
    vi.mocked(get).mockResolvedValue({
      success: true,
      data: {
        average: 0,
        count: 0,
        distribution: [],
        total: 0,
        page: 0,
        pageSize: 10,
        reviews: [],
        myReview: { rating: 4, text: '' },
      },
      errorMessage: null,
    })
    vi.mocked(put).mockResolvedValue(ok)
    const { host, mount } = await hydrate(VariantReviews)
    host.querySelector('textarea')!.value = 'typed before the chunk'
    mount()
    await settle()

    ;(host.querySelector('[data-testid="submit-review"]') as HTMLButtonElement).click()
    await settle()

    expect(put).toHaveBeenCalledWith(`/api/variant/${GUID}/review`, { rating: 4, text: 'typed before the chunk' })
  })

  it('sends the saved review text when nothing was typed', async () => {
    vi.mocked(get).mockResolvedValue({
      success: true,
      data: {
        average: 0,
        count: 0,
        distribution: [],
        total: 0,
        page: 0,
        pageSize: 10,
        reviews: [],
        myReview: { rating: 4, text: 'Saved' },
      },
      errorMessage: null,
    })
    vi.mocked(put).mockResolvedValue(ok)
    const { host, mount } = await hydrate(VariantReviews)
    mount()
    await settle()

    ;(host.querySelector('[data-testid="submit-review"]') as HTMLButtonElement).click()
    await settle()

    expect(put).toHaveBeenCalledWith(`/api/variant/${GUID}/review`, { rating: 4, text: 'Saved' })
  })
})
