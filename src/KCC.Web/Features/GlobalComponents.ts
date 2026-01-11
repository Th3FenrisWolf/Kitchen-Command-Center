import type { App } from 'vue'
import AppHeader from '~/Components/Header/AppHeader.vue'
import AppFooter from '~/Components/Footer/Footer.vue'
import AppLink from '~/Components/Links/AppLink.vue'
import UnderlineLink from '~/Components/Links/UnderlineLink.vue'
import Card from '~/Widgets/Card/Card.vue'
import Stacker from '~/Widgets/Stacker/Stacker.vue'

// Register all global components here
// Components are registered with both PascalCase and lowercase names
// because HTML parsers lowercase custom element tags, but Vue templates use PascalCase
const globalComponents: Record<string, object> = {
  AppHeader,
  AppFooter,
  AppLink,
  UnderlineLink,
  Card,
  Stacker,
}

export function registerGlobalComponents(app: App) {
  Object.entries(globalComponents).forEach(([name, component]) => {
    // Register with PascalCase (for Vue templates)
    app.component(name, component)
    // Register with lowercase (for HTML-parsed templates from SSR)
    app.component(name.toLowerCase(), component)
  })
}
