import type { App, Component } from 'vue'

const componentModules = import.meta.glob<{ default: Component }>('./**/*.Component.vue', { eager: true })

function getComponentName(path: string): string | null {
  const match = path.match(/\/([^/]+)\.Component\.vue$/)
  return match?.[1] ?? null
}

// Registered under both names: HTML parsers lowercase custom element tags, but Vue templates use PascalCase.
export const registerGlobalComponents = (app: App) => {
  Object.entries(componentModules).forEach(([path, module]) => {
    const name = getComponentName(path)
    if (!name || !module.default) return

    app.component(name, module.default)
    app.component(name.toLowerCase(), module.default)
  })
}
