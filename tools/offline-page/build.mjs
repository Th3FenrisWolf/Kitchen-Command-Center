// Writes dist/worker.js, the one file pasted into the Cloudflare dashboard, and dist/preview.html, the page with
// its fonts inline: node tools/offline-page/build.mjs
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const styles = join(here, '../../src/KCC.Web/Features/Styles')

const fontFiles = {
  casual: 'AmericanPurposeCasualRegular.woff2',
  hazelnut: 'HazelnutMilkTeaRegular.woff2',
  sono: 'Sono-Regular-latin.woff2',
}

export function render() {
  const fonts = Object.fromEntries(
    Object.entries(fontFiles).map(([key, file]) => [key, readFileSync(join(styles, 'fonts', file)).toString('base64')]),
  )
  const tears = readFileSync(join(styles, 'Torn/Tears.css'), 'utf8')

  function tear(name) {
    const match = tears.match(new RegExp(`--tear-${name}: (polygon\\([^;]*\\));`))
    if (!match) {
      throw new Error(`Tears.css has no --tear-${name}`)
    }
    return match[1]
  }

  function page(fontUrl) {
    return readFileSync(join(here, 'offline.html'), 'utf8')
      .replace(/\{\{FONT:(\w+)\}\}/g, (_, key) => {
        if (!Object.hasOwn(fonts, key)) {
          throw new Error(`offline.html names an unknown font: ${key}`)
        }
        return fontUrl(key)
      })
      .replace(/\{\{TEAR:(\w+)\}\}/g, (_, name) => tear(name))
  }

  function fill(source, marker, value) {
    const placeholder = new RegExp(`/\\*${marker}\\*/ (?:''|""|\\{\\})`)
    if (!placeholder.test(source)) {
      throw new Error(`worker.template.js has no /*${marker}*/ placeholder`)
    }
    return source.replace(placeholder, () => JSON.stringify(value))
  }

  const template = readFileSync(join(here, 'worker.template.js'), 'utf8')
  const worker = fill(
    fill(
      template,
      'PAGE',
      page((key) => `/__kcc-offline/${key}.woff2`),
    ),
    'FONTS',
    Object.fromEntries(Object.entries(fonts).map(([key, data]) => [`${key}.woff2`, data])),
  )

  return {
    worker,
    preview: page((key) => `data:font/woff2;base64,${fonts[key]}`),
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const { worker, preview } = render()
  mkdirSync(join(here, 'dist'), { recursive: true })
  writeFileSync(join(here, 'dist/worker.js'), worker)
  writeFileSync(join(here, 'dist/preview.html'), preview)
  console.log(`dist/worker.js: ${(worker.length / 1024).toFixed(0)} KiB`)
}
