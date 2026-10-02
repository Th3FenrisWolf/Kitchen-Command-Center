interface SentinelEntry {
  target: { nextElementSibling: { classList: Pick<DOMTokenList, 'toggle'> } | null }
  boundingClientRect: Pick<DOMRectReadOnly, 'top'>
}

// A fast scroll moves several sentinels past the threshold in one frame, and the observer reports them in one call.
export const markStuckCards = (entries: SentinelEntry[]) => {
  entries.forEach(({ target, boundingClientRect }) => {
    target.nextElementSibling?.classList.toggle('stuck', boundingClientRect.top < 0)
  })
}
