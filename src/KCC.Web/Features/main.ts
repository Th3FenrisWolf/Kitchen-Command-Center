/* eslint-disable no-console */

import '~/Styles/Main.css'

import { createSSRApp, h } from 'vue'
import App from '~/App.vue'
import { registerGlobalComponents } from '~/register-global-components'

// Get the server content from the script tag (stored as JSON to prevent XSS)
const serverContentEl = document.getElementById('server-content')
let serverContent = ''

if (serverContentEl?.textContent) {
  try {
    // Parse the JSON-encoded server content
    serverContent = JSON.parse(serverContentEl.textContent)
  } catch (e) {
    console.error('Failed to parse server content:', e)
  }
}

// Create SSR app for hydration (reuses existing DOM instead of replacing it)
const app = createSSRApp({
  setup() {
    return () => h(App, { serverContent })
  },
})

registerGlobalComponents(app)

// Hydrate the SSR-rendered HTML in #app
// This attaches event listeners without re-rendering the DOM
app.mount('#app')
