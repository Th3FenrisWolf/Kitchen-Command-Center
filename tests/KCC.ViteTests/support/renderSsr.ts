import { createSSRApp, h, type Component, type Slots } from 'vue'
import { renderToString } from '@vue/server-renderer'

/**
 * Renders a component the way the real entries would.
 *
 * The app registers every `*.Component.vue` under `Features/` app-wide (`GlobalComponents.ts`), so a page
 * template can name one without importing it. A bare `renderToString(createSSRApp(C))` has no such
 * registry: pass the ones a page under test uses as `globals`, or Vue warns and renders nothing where the
 * tag was.
 */
export function renderSsr(
  component: Component,
  props?: Record<string, unknown>,
  slots?: Record<string, () => unknown>,
  globals?: Record<string, Component>,
): Promise<string> {
  const app = createSSRApp({ render: () => h(component, props ?? {}, slots as unknown as Slots) })
  Object.entries(globals ?? {}).forEach(([name, global]) => app.component(name, global))
  return renderToString(app)
}
