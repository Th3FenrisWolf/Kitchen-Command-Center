import { ARRIVAL_TIMEOUT_MS } from './laterParts'

export const PENDING_ATTRIBUTE = 'data-later-pending'
const PENDING_CHILD = `:scope > [${PENDING_ATTRIBUTE}]`

// Watches only the holder's own children: a pending root is one of them, and is replaced when its part arrives or fails.
export function whenArrived(holder: Element | null | undefined): Promise<void> {
  if (!holder?.querySelector(PENDING_CHILD)) return Promise.resolve()

  return new Promise((resolve) => {
    const done = () => {
      observer.disconnect()
      clearTimeout(timeout)
      resolve()
    }
    const observer = new MutationObserver(() => {
      if (!holder.querySelector(PENDING_CHILD)) done()
    })
    const timeout = setTimeout(done, ARRIVAL_TIMEOUT_MS)
    observer.observe(holder, { childList: true })
  })
}
