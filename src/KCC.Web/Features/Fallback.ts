// Must stay the first import so dev error listeners exist before the app mounts.
import '~/DevTools/RuntimeErrorOverlay'
import { createApp, h, nextTick } from 'vue'
import App from '~/App.vue'
import { BLOOM_KEY, createBloom } from '~/Bloom/bloomContext'
import { startVisitHeartbeat } from '~/Bloom/visitHeartbeat'
import { mountedAfterBeforeHydrate } from '~/Islands/beforeHydrate'
import { scheduleLaterParts } from '~/Islands/laterSchedule'
import { eagerGlobals } from '~/Ssr/ServerComponents'
import type { SsrPayload } from '~/Types/ContentRegions'

import '~/Utilities/StringExtensions'
import { configureApi, type ApiStrings } from '~/Utilities/Api'

const serverContentEl = document.getElementById('server-content')
if (!serverContentEl?.textContent) throw new Error('Server content not found')

const { isPreview, bloom: inputs, ...contentRegions } = JSON.parse(serverContentEl.textContent) as SsrPayload

// Before mount, so a form handler firing right after the mount already has the token.
const apiConfigEl = document.getElementById('api-config')
if (apiConfigEl?.textContent) {
  configureApi(JSON.parse(apiConfigEl.textContent) as { antiforgeryToken?: string; strings?: ApiStrings })
}

const app = createApp({
  setup: () => () => h(App, { ...contentRegions }),
})

const bloom = createBloom(inputs.seed, inputs.page)

// HTML parsers lowercase custom element tags, but the region templates use PascalCase too.
const settling: Promise<void>[] = []
for (const { name, component } of eagerGlobals) {
  const registered = mountedAfterBeforeHydrate(component, name, (settled) => settling.push(settled))
  app.component(name, registered)
  app.component(name.toLowerCase(), registered)
}
app.provide('isPreview', isPreview ?? false)
app.provide(BLOOM_KEY, bloom)
app.mount('#app')

// Each settled island renders in the next flush, and the page is usable only once it has.
void Promise.all(settling)
  .then(() => nextTick())
  .then(() => {
    performance.mark('kcc:hydrated')
    bloom.markHydrated()
    startVisitHeartbeat()
    scheduleLaterParts()
  })
