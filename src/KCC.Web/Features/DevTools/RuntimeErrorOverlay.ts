// Vite only pops its error overlay for compile-time errors pushed over the HMR
// socket; runtime errors and SSR render failures (re-emitted by SsrHtmlContent
// as #ssr-error) would otherwise reach the console only. The vite-error-overlay
// element is registered by @vite/client, which Vite.AspNetCore injects into
// every page while the dev server is running.

type OverlayError = { message: string; stack: string }
type ErrorOverlayConstructor = new (err: OverlayError) => HTMLElement

if (import.meta.env.DEV) {
  const toOverlayError = (error: unknown): OverlayError =>
    error instanceof Error ? { message: error.message, stack: error.stack ?? '' } : { message: String(error), stack: '' }

  const showOverlay = (error: OverlayError) => {
    const ErrorOverlay = customElements.get('vite-error-overlay') as ErrorOverlayConstructor | undefined

    // Keep the first overlay: follow-up errors are usually cascade noise.
    if (!ErrorOverlay || document.querySelector('vite-error-overlay')) {
      return
    }

    document.body.appendChild(new ErrorOverlay(error))
  }

  window.addEventListener('error', (event) => showOverlay(toOverlayError(event.error ?? event.message)))
  window.addEventListener('unhandledrejection', (event) => showOverlay(toOverlayError(event.reason)))

  const ssrErrorJson = document.getElementById('ssr-error')?.textContent
  if (ssrErrorJson) {
    const { message, stack } = JSON.parse(ssrErrorJson) as { message: string; stack?: string }
    showOverlay({ message: `[SSR render failed] ${message}`, stack: stack ?? '' })
  }
}
