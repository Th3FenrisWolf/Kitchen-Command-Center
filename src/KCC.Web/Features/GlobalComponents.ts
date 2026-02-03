import type { App, Component } from 'vue'

// Auto-discover global components using Vite's glob import
// Only files matching *.Component.vue are registered globally
const componentModules = import.meta.glob<{ default: Component }>('./**/*.Component.vue', {
  eager: true,
})

// Extract component name from file path
// e.g., './Components/Header/AppHeader.component.vue' -> 'AppHeader'
function getComponentName(path: string): string | null {
  const match = path.match(/\/([^/]+)\.Component\.vue$/)
  return match?.[1] ?? null
}

// Register all global components
// Components are registered with both PascalCase and lowercase names
// because HTML parsers lowercase custom element tags, but Vue templates use PascalCase
export const registerGlobalComponents = (app: App) =>
  Object.entries(componentModules).forEach(([path, module]) => {
    const name = getComponentName(path)
    if (!name || !module.default) return

    app.component(name, module.default)
    app.component(name.toLowerCase(), module.default)
  })
