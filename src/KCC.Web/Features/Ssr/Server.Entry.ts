import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { registerGlobalComponents } from '~/GlobalComponents'
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

  registerGlobalComponents(app)
  app.provide('isPreview', isPreview ?? false)
  app.provide(BLOOM_KEY, bloom)
  return { app, bloom }
}
