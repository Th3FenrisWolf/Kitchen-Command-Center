/* eslint-disable no-console */

import '~/Styles/Main.css'
import '~/Utilities/StringExtensions'

import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/GlobalComponents'

// Content regions from server
interface ContentRegions {
  headerContent: string
  bodyContent: string
  footerContent: string
}

// Get the server content from the script tag (stored as JSON to prevent XSS)
const serverContentEl = document.getElementById('server-content')
let contentRegions: ContentRegions = {
  headerContent: '',
  bodyContent: '',
  footerContent: '',
}

if (serverContentEl?.textContent) {
  try {
    // Parse the JSON-encoded server content regions
    contentRegions = JSON.parse(serverContentEl.textContent)
  } catch (e) {
    console.error('Failed to parse server content:', e)
  }
}

// Create SSR app for hydration (reuses existing DOM instead of replacing it)
const app = createSSRApp({
  setup() {
    return () =>
      h(App, {
        headerContent: contentRegions.headerContent,
        bodyContent: contentRegions.bodyContent,
        footerContent: contentRegions.footerContent,
      })
  },
})

registerGlobalComponents(app)

// Hydrate the SSR-rendered HTML in #app
// This attaches event listeners without re-rendering the DOM
app.mount('#app')
