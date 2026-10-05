const BASE = 'http://pad.invalid'

function pathWithoutTrailingSlash(url: URL) {
  return url.pathname === '/' ? url.pathname : url.pathname.replace(/\/$/, '')
}

function sortedQuery(url: URL) {
  url.searchParams.sort()
  return url.searchParams.toString()
}

export function isCurrentPage(url: string | undefined, currentPage: string | undefined): boolean {
  if (!url || !currentPage) {
    return false
  }
  try {
    const link = new URL(url, BASE)
    const page = new URL(currentPage, BASE)
    return (
      link.origin === page.origin &&
      pathWithoutTrailingSlash(link) === pathWithoutTrailingSlash(page) &&
      sortedQuery(link) === sortedQuery(page)
    )
  } catch {
    return false
  }
}
