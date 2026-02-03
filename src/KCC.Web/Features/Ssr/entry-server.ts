import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/register-global-components'

export interface SSRContext {
  serverContent: string
}

export function createApp(context: SSRContext) {
  const app = createSSRApp({
    setup() {
      return () => h(App, { serverContent: context.serverContent })
    },
  })

  registerGlobalComponents(app)

  return app
}
