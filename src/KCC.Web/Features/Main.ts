// Must stay the first import so dev error listeners exist before hydration runs.
import '~/DevTools/RuntimeErrorOverlay'
import { createBloom } from '~/Bloom/bloomContext'
import { startVisitHeartbeat } from '~/Bloom/visitHeartbeat'
import { hydrateIslands } from '~/Islands/hydrate'
import { scheduleLaterParts } from '~/Islands/laterSchedule'
import type { IslandPayload } from '~/Types/ContentRegions'

import '~/Utilities/StringExtensions'
import { configureApi, type ApiStrings } from '~/Utilities/Api'

const serverContentEl = document.getElementById('server-content')
if (!serverContentEl?.textContent) throw new Error('Server content not found')

const payload = JSON.parse(serverContentEl.textContent) as IslandPayload

// Before any island mounts, so a form handler firing right after hydration already has the token.
const apiConfigEl = document.getElementById('api-config')
if (apiConfigEl?.textContent) {
  configureApi(JSON.parse(apiConfigEl.textContent) as { antiforgeryToken?: string; strings?: ApiStrings })
}

const bloom = createBloom(payload.bloom.seed, payload.bloom.page)

void hydrateIslands({ ...payload, islands: payload.islands ?? [] }, bloom).then(() => {
  performance.mark('kcc:hydrated')
  bloom.markHydrated(payload.bloom.pageWashes)
  startVisitHeartbeat()
  scheduleLaterParts()

  // Each island's import injected its component styles into <head>. The server's copy sits in <body>, later in
  // document order, so leaving it would outrank HMR-updated head styles and make style edits look broken until a
  // full reload. Removed only after the last island, so an island whose chunk failed is styled until then.
  document.querySelectorAll('style[data-ssr-styles]').forEach((el) => el.remove())
})
