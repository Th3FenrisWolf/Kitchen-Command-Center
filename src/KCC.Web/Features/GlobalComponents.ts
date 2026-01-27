import type { App, Component } from 'vue'

// Auto-discover global components using Vite's glob import
// Only files matching *.component.vue are registered globally
const componentModules = import.meta.glob<{ default: Component }>(
  './**/*.component.vue',
  { eager: true },
)

// Extract component name from file path
// e.g., './Components/Header/AppHeader.component.vue' -> 'AppHeader'
function getComponentName(path: string): string | null {
  const match = path.match(/\/([^/]+)\.component\.vue$/)
  return match?.[1] ?? null
}

// Register all global components
// Components are registered with both PascalCase and lowercase names
// because HTML parsers lowercase custom element tags, but Vue templates use PascalCase
export function registerGlobalComponents(app: App) {
  for (const [path, module] of Object.entries(componentModules)) {
    const name = getComponentName(path)
    if (name && module.default) {
      // Register with PascalCase (for Vue templates)
      app.component(name, module.default)
      // Register with lowercase (for HTML-parsed templates from SSR)
      app.component(name.toLowerCase(), module.default)
    }
  }
}
