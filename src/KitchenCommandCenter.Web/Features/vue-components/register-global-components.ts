import type { App } from 'vue'
import Test from './Test.vue'
import UnderlineLink from './shared/UnderlineLink.vue'
import Card from '~/Widgets/Card/Card.vue'

// Register all global components here
// Import your Vue components and add them to this object
const globalComponents = {
  test: Test,
  underlinelink: UnderlineLink,
  card: Card,
  // Add more components here:
  // myComponent: MyComponent,
  // anotherComponent: AnotherComponent,
}

export function registerGlobalComponents(app: App) {
  Object.entries(globalComponents).forEach(([name, component]) => {
    app.component(name, component)
  })
}
