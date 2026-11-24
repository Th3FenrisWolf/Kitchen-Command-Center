import { camelize, defineComponent, h, shallowRef, type Component } from 'vue'

type BeforeHydrate = (props: Record<string, unknown>) => Promise<unknown>

declare module 'vue' {
  interface ComponentCustomOptions {
    /** Settles before the island hydrates, so a later part the server rendered first is not hydrated as pending. */
    beforeHydrate?: BeforeHydrate
  }
}

const beforeHydrateOf = (component: Component) => (component as { beforeHydrate?: BeforeHydrate }).beforeHydrate

// Razor writes an island's props in kebab case, and they reach here as written, before Vue resolves them.
const asDeclared = (props: Record<string, unknown>) =>
  Object.fromEntries(Object.entries(props).map(([key, value]) => [camelize(key), value]))

// Answers whether every part arrived. One that did not shows its own error state once the island is live, but only
// in an island rendered afresh: Vue hydrates an async part by waiting for its load, and a failed load leaves the
// server's HTML for that part inert.
export async function settleBeforeHydrate(component: Component, props: Record<string, unknown>, name: string) {
  try {
    await beforeHydrateOf(component)?.(asDeclared(props))
    return true
  } catch (error) {
    // eslint-disable-next-line no-console
    console.warn(`Island ${name} goes live without a part it waits for.`, error)
    return false
  }
}

// The fallback renders islands from the region templates, so their props are known only once each one sets up.
export function mountedAfterBeforeHydrate(
  component: Component,
  name: string,
  settling: (settled: Promise<void>) => void,
): Component {
  if (!beforeHydrateOf(component)) return component

  return defineComponent({
    name,
    inheritAttrs: false,
    setup(_, { attrs, slots }) {
      const settled = shallowRef(false)
      settling(
        settleBeforeHydrate(component, attrs, name).then(() => {
          settled.value = true
        }),
      )
      return () => (settled.value ? h(component, attrs, slots) : null)
    },
  })
}
