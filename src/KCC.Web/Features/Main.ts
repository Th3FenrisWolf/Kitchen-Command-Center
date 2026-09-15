// Must stay the first import so dev error listeners exist before hydration runs.
import '~/DevTools/RuntimeErrorOverlay'
import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import type { SsrPayload } from '~/Types/ContentRegions'

import '~/Utilities/StringExtensions'
import { configureApi, type ApiStrings } from '~/Utilities/Api'

// Get the server content from the script tag (stored as JSON to prevent XSS)
const serverContentEl = document.getElementById('server-content')
if (!serverContentEl?.textContent) throw new Error('Server content not found')

// Parse the JSON-encoded server content
const { isPreview, ...contentRegions } = JSON.parse(serverContentEl.textContent) as SsrPayload
if (!contentRegions) throw new Error('Failed to parse server content')

// Seed the API utility with the antiforgery token + localized fallback strings
// that Layout.cshtml emits once into #api-config. Done before mount so any form
// handler firing after hydration has the token ready.
const apiConfigEl = document.getElementById('api-config')
if (apiConfigEl?.textContent) {
  configureApi(JSON.parse(apiConfigEl.textContent) as { antiforgeryToken?: string; strings?: ApiStrings })
}

// Create SSR app for hydration (reuses existing DOM instead of replacing it)
const app = createSSRApp({
  setup: () => () => h(App, { ...contentRegions }),
})

// Hydrate the SSR-rendered HTML in #app
// Attach event listeners without re-rendering the DOM
registerGlobalComponents(app)
app.provide('isPreview', isPreview ?? false)
app.mount('#app')

// Vite injected every component style into <head> during module evaluation. The server's copy sits
// in <body>, later in document order, so leaving it would outrank HMR-updated head styles and make
// style edits look broken until a full reload. Only reached once hydration succeeds, so a throw
// above leaves the page styled.
document.querySelectorAll('style[data-ssr-styles]').forEach((el) => el.remove())
