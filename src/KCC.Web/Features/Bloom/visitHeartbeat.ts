const COOKIE = 'kcc-visit'
const BEAT_MS = 60_000
const MAX_AGE_S = 1800

function currentVisit(): string | undefined {
  for (const pair of document.cookie.split(';')) {
    const [name, ...value] = pair.trim().split('=')
    if (name === COOKIE) return value.join('=')
  }
  return undefined
}

function renewVisit(): void {
  const value = currentVisit()
  if (value === undefined) return
  const secure = location.protocol === 'https:' ? '; secure' : ''
  document.cookie = `${COOKIE}=${value}; max-age=${MAX_AGE_S}; path=/; samesite=lax${secure}`
}

export function startVisitHeartbeat(): () => void {
  const timer = setInterval(renewVisit, BEAT_MS)
  window.addEventListener('pagehide', renewVisit)
  return () => {
    clearInterval(timer)
    window.removeEventListener('pagehide', renewVisit)
  }
}
