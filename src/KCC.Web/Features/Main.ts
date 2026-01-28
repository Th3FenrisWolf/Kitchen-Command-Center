import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'
import type { ContentRegions } from '~/Types/ContentRegions'

import '~/Styles/Main.css'
import '~/Utilities/StringExtensions'

// Get the server content from the script tag (stored as JSON to prevent XSS)
const serverContentEl = document.getElementById('server-content')
if (!serverContentEl?.textContent) throw new Error('Server content not found')

// Parse the JSON-encoded server content regions
const contentRegions = JSON.parse(serverContentEl.textContent) as ContentRegions
if (!contentRegions) throw new Error('Failed to parse server content')

// Create SSR app for hydration (reuses existing DOM instead of replacing it)
const app = createSSRApp({
  setup: () => () => h(App, { ...contentRegions }),
})

// Hydrate the SSR-rendered HTML in #app
// This attaches event listeners without re-rendering the DOM
registerGlobalComponents(app)
app.mount('#app')
