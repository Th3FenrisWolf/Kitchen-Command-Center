import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { isGlobalKey } from '~/Bloom/washes'
import { eagerGlobals } from './ServerComponents'
import type { RenderOutput } from '~/Islands/payload'
import { registerIslands } from '~/Islands/ssrIslands'
import type { SsrPayload } from '~/Types/ContentRegions'

import '~/Styles/Main.css'
import '~/Utilities/StringExtensions'

export { scheduleWashes } from '~/Bloom/washes'

export function createApp(payload: SsrPayload) {
  const { isPreview, bloom: inputs, ...contentRegions } = payload

  const app = createSSRApp({
    setup: () => () => h(App, { ...contentRegions }),
  })

  const bloom = createBloom(inputs.seed, inputs.page)

  const islands = registerIslands(app, eagerGlobals)
  app.provide('isPreview', isPreview ?? false)
  app.provide(BLOOM_KEY, bloom)

  const output = (): RenderOutput => ({
    ...islands,
    pageWashes: bloom.collected().filter((wash) => !isGlobalKey(wash.key)).length,
  })

  return { app, bloom, output }
}
