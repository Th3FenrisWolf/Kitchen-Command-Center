import { createSSRApp, h, type Component, type Slots } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { expect } from 'vitest'
import { RETIRED } from './retired'

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

// Whole opening tags, so an assertion about one element's classes and hooks does not pin the order Vue prints its
// attributes in.
export const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

export const openTag = (html: string, needle: string) => {
  const at = html.indexOf(needle)
  expect(at, `${needle} is not in the render`).toBeGreaterThan(-1)
  return html.slice(html.lastIndexOf('<', at), html.indexOf('>', at) + 1)
}

// What useResourceStrings hands back for a key with no value.
export const echoKey = (key: string) => key

export const expectNoRetiredMarkup = (html: string) => expect(html).not.toMatch(RETIRED)
