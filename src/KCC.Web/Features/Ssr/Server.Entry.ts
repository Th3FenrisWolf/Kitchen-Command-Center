import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import type { SsrPayload } from '~/Types/ContentRegions'

import '~/Styles/Main.css'
import '~/Utilities/StringExtensions'

export function createApp(payload: SsrPayload) {
  const { isPreview, ...contentRegions } = payload

  const app = createSSRApp({
    setup: () => () => h(App, { ...contentRegions }),
  })

  registerGlobalComponents(app)
  app.provide('isPreview', isPreview ?? false)
  return app
}
