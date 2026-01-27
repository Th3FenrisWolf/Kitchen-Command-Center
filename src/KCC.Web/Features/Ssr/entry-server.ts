import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import '~/Utilities/StringExtensions'

// CSS import is required for Tailwind to scan templates during SSR dev mode
import '~/Styles/Main.css'

export interface SSRContext {
  headerContent: string
  bodyContent: string
  footerContent: string
}

export function createApp(context: SSRContext) {
  const app = createSSRApp({
    setup() {
      return () =>
        h(App, {
          headerContent: context.headerContent,
          bodyContent: context.bodyContent,
          footerContent: context.footerContent,
        })
    },
  })

  registerGlobalComponents(app)

  return app
}
