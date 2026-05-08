import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import { vResourceKey } from '~/ResourceStringEditing/vResourceKey'
import type { ContentRegions } from '~/Types/ContentRegions'

import '~/Styles/Main.css'
import '~/Utilities/StringExtensions'

export function createApp(contentRegions: ContentRegions) {
  const app = createSSRApp({
    setup: () => () => h(App, { ...contentRegions }),
  })

  registerGlobalComponents(app)
  app.directive('resource-key', vResourceKey)
  return app
}
