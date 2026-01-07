import type { App } from 'vue'
import Test from './Test.vue'
import UnderlineLink from './shared/UnderlineLink.vue'
import Card from '~/Widgets/Card/Card.vue'

// Register all global components here
// Components are registered with both PascalCase and lowercase names
// because HTML parsers lowercase custom element tags, but Vue templates use PascalCase
const globalComponents: Record<string, object> = {
  Test,
  UnderlineLink,
  Card,
  // Add more components here:
  // MyComponent,
  // AnotherComponent,
}

export function registerGlobalComponents(app: App) {
  Object.entries(globalComponents).forEach(([name, component]) => {
    // Register with PascalCase (for Vue templates)
    app.component(name, component)
    // Register with lowercase (for HTML-parsed templates from SSR)
    app.component(name.toLowerCase(), component)
  })
}
