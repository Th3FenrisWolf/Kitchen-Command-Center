import { createSSRApp, h, type Component, type Slots } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { vInk } from '~/Ink/vInk'

/**
 * Renders a component the way the real entries would.
 *
 * `v-ink` is registered on the app, not globally, so a bare `renderToString(createSSRApp(C))` fails to
 * resolve it for any component that draws an outline — the component renders, but Vue logs
 * "Failed to resolve directive: ink" and the assertion sees markup the app would never produce. Kept while
 * any tested component still carries `v-ink` (`grep -rl v-ink Features/Components` — RecipeCard,
 * FeaturedRecipeCard, DetailHero and RecipeFilters at the time of writing); drop it once the Softbound ink
 * module retires in the cleanup phase.
 */
export function renderSsr(
  component: Component,
  props?: Record<string, unknown>,
  slots?: Record<string, () => unknown>,
): Promise<string> {
  const app = createSSRApp({ render: () => h(component, props ?? {}, slots as unknown as Slots) })
  app.directive('ink', vInk)
  return renderToString(app)
}
