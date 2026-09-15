const CSS_RE = /\.(css|less|sass|scss|styl|stylus|pcss|postcss|sss)(?:$|\?)/

// Vite records module file paths with posix separators regardless of platform.
const normalize = (path) => path?.replace(/\\/g, '/')

// '?direct' makes vite:css-post return compiled CSS text instead of the JS module that injects it.
const toDirectUrl = (url) => url + (url.includes('?') ? '&direct' : '?direct')

/**
 * @param prunedFiles Absolute paths whose subtrees are skipped. A CSS file's `@import`s are its
 * children in the graph, so pruning an entry drops everything it pulls in with it.
 */
export function collectCssUrls(graph, entryFile, prunedFiles = []) {
  const pruned = new Set([...prunedFiles].map(normalize))
  const seen = new Set()
  const urls = []

  const walk = (mod) => {
    const key = mod.id ?? mod.url
    if (seen.has(key) || pruned.has(normalize(mod.file))) return

    seen.add(key)

    if (CSS_RE.test(mod.url)) urls.push(mod.url)

    for (const dep of mod.importedModules) walk(dep)
  }

  for (const mod of graph.getModulesByFile(normalize(entryFile)) ?? []) walk(mod)

  return urls
}

export async function collectCss(graph, transformRequest, entryFile, prunedFiles) {
  const chunks = await Promise.all(
    collectCssUrls(graph, entryFile, prunedFiles).map(async (url) => (await transformRequest(toDirectUrl(url)))?.code),
  )

  return chunks.filter(Boolean).join('\n')
}
