// Injected into admin SPA HTML responses by AdminHomePageMiddleware.
// Forces client-side navigation targeting /admin (the admin chrome's home
// button uses pushState, which the server never sees) over to our custom
// Home application instead of Kentico's default dashboard.
;(function () {
  var TARGET = '/admin/home'

  function shouldRedirect(url) {
    if (!url) return false
    try {
      var parsed = new URL(url, window.location.origin)
      return parsed.pathname === '/admin' || parsed.pathname === '/admin/'
    } catch (err) {
      _ = err
      return false
    }
  }

  document.addEventListener(
    'click',
    function (event) {
      var target = event.target
      if (!target || typeof target.closest !== 'function') return

      var anchor = target.closest('a')
      if (anchor && shouldRedirect(anchor.getAttribute('href'))) {
        event.preventDefault()
        event.stopImmediatePropagation()
        window.location.href = TARGET
      }
    },
    true,
  )

  var originalPush = history.pushState
  history.pushState = function (state, title, url) {
    if (shouldRedirect(url)) {
      window.location.href = TARGET
      return
    }
    return originalPush.call(this, state, title, url)
  }

  var originalReplace = history.replaceState
  history.replaceState = function (state, title, url) {
    if (shouldRedirect(url)) {
      window.location.href = TARGET
      return
    }
    return originalReplace.call(this, state, title, url)
  }
})()
