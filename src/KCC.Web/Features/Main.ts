import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import type { SsrPayload } from '~/Types/ContentRegions'

import '~/Utilities/StringExtensions'

// Get the server content from the script tag (stored as JSON to prevent XSS)
const serverContentEl = document.getElementById('server-content')
if (!serverContentEl?.textContent) throw new Error('Server content not found')

// Parse the JSON-encoded server content
const { isPreview, ...contentRegions } = JSON.parse(serverContentEl.textContent) as SsrPayload
if (!contentRegions) throw new Error('Failed to parse server content')

// Create SSR app for hydration (reuses existing DOM instead of replacing it)
const app = createSSRApp({
  setup: () => () => h(App, { ...contentRegions }),
})

// Hydrate the SSR-rendered HTML in #app
// Attach event listeners without re-rendering the DOM
registerGlobalComponents(app)
app.provide('isPreview', isPreview ?? false)
app.mount('#app')
