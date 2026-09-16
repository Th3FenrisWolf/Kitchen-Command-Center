import { createSSRApp, type Component } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { vInk } from '~/Ink/vInk'

/**
 * Renders a component with the app-scoped registrations the real entries install.
 *
 * `v-ink` is registered on the app, not globally, so a bare `renderToString(createSSRApp(C))` fails to
 * resolve it for any component that draws an outline — the component renders, but Vue logs
 * "Failed to resolve directive: ink" and the assertion sees markup the app would never produce. Only the
 * directive is registered here rather than all of `registerGlobalComponents`, which would eager-import
 * every `*.Component.vue` through a Vite glob for no benefit to a single-component test.
 */
export function renderSsr(component: Component, props?: Record<string, unknown>): Promise<string> {
  const app = createSSRApp(component, props)
  app.directive('ink', vInk)
  return renderToString(app)
}
