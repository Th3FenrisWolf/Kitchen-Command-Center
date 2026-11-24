// Throwaway FR-003 probe: mirrors the perf harness's phone case and records Event Timing entries and a trace.
import { createRequire } from 'node:module'
import { writeFileSync } from 'node:fs'
const require = createRequire(import.meta.url)
const { chromium } = require(process.env.HOME + '/.nuget/packages/microsoft.playwright/1.64.1/.playwright/package/index.js')

const BASE = 'http://127.0.0.1:58672'
const OUT = new URL('./traces/', import.meta.url).pathname
const [page_ = '/', mode = 'harness', label = 'x', profileName = process.env.PROFILE ?? 'phone', visitAgo = process.env.AGO ?? '400', motion = process.env.MOTION ?? 'full'] = process.argv.slice(2)
const phone = profileName === 'phone'

const observers = `(() => {
  const state = { events: [], loaf: [], shifts: [] }
  try { new PerformanceObserver((l) => l.getEntries().forEach((e) => state.events.push({ name: e.name, start: e.startTime, ps: e.processingStart, pe: e.processingEnd, dur: e.duration, id: e.interactionId, target: e.target ? (e.target.tagName + '.' + String(e.target.className).slice(0, 60)) : null }))).observe({ type: 'event', buffered: true, durationThreshold: 16 }) } catch {}
  try { new PerformanceObserver((l) => l.getEntries().forEach((e) => state.loaf.push({ start: e.startTime, dur: e.duration, block: e.blockingDuration, render: e.renderStart, style: e.styleAndLayoutStart, scripts: e.scripts.map((s) => ({ inv: s.invoker, src: (s.sourceURL || '').split('/').pop() + ':' + s.sourceFunctionName, dur: Math.round(s.duration), fsl: Math.round(s.forcedStyleAndLayoutDuration) })) }))).observe({ type: 'long-animation-frame', buffered: true }) } catch {}
  try { new PerformanceObserver((l) => l.getEntries().forEach((e) => { if (!e.hadRecentInput) state.shifts.push({ v: e.value, start: e.startTime }) })).observe({ type: 'layout-shift', buffered: true }) } catch {}
  state.frames = []
  state.rec = () => { state.frames = []; let last = performance.now(); state.on = true; const t = (now) => { state.frames.push(now - last); last = now; if (state.on) requestAnimationFrame(t) }; requestAnimationFrame((n) => { last = n; requestAnimationFrame(t) }) }
  window.__p = state
})()`

const gpuArgs = process.env.GPU === 'metal' ? ['--use-angle=metal', '--enable-gpu', '--ignore-gpu-blocklist', '--enable-gpu-rasterization'] : ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader']
const browser = await chromium.launch({ headless: true, args: gpuArgs })
const context = await browser.newContext({
  viewport: phone ? { width: 390, height: 844 } : { width: 1440, height: 900 },
  deviceScaleFactor: phone ? 3 : 1, isMobile: phone, hasTouch: phone,
  reducedMotion: motion === 'reduced' ? 'reduce' : 'no-preference',
})
const started = Math.floor(Date.now() / 1000) - Number(visitAgo)
await context.addCookies([{ name: 'kcc-visit', value: `1.0badf00d.${started}`, domain: '127.0.0.1', path: '/', sameSite: 'Lax' }])
await context.addInitScript(observers)
if (process.env.CSS) await context.addInitScript(`document.addEventListener('DOMContentLoaded', () => { const s = document.createElement('style'); s.textContent = ${JSON.stringify(process.env.CSS)}; document.head.append(s) })`)
const page = await context.newPage()
const cdp = await context.newCDPSession(page)
if (phone) {
  await cdp.send('Network.enable')
  await cdp.send('Network.emulateNetworkConditions', { offline: false, latency: 170, downloadThroughput: 9_000_000 / 8, uploadThroughput: 1_500_000 / 8 })
  await cdp.send('Emulation.setCPUThrottlingRate', { rate: 4 })
}

const events = []
cdp.on('Tracing.dataCollected', (d) => events.push(...d.value))
const done = new Promise((r) => cdp.on('Tracing.tracingComplete', r))
const traceFromLoad = mode.includes('loadtrace')
const cats = (process.env.CATS ? process.env.CATS + ',' : '') + 'toplevel,devtools.timeline,disabled-by-default-devtools.timeline,disabled-by-default-devtools.timeline.frame,benchmark,cc,viz,v8.execute,blink.user_timing,loading'
if (traceFromLoad) await cdp.send('Tracing.start', { categories: cats, transferMode: 'ReportEvents' })

await page.goto(BASE + page_, { waitUntil: 'load' })
await page.waitForFunction(() => performance.getEntriesByName('kcc:hydrated').length > 0, null, { timeout: 90000 })
const hydrated = await page.evaluate(() => performance.getEntriesByName('kcc:hydrated')[0].startTime)
const result = { page: page_, mode, label, profileName, hydrated }

if (mode.startsWith('early')) {
  // First input right after usable, no settle wait.
  await page.mouse.move(195, 420)
  await page.waitForTimeout(1500)
} else {
  await page.evaluate(async () => { const f = () => new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r))); for (let i = 0; i < 20; i++) await f() })
  await page.evaluate(() => window.scrollTo(0, 0))
  await page.waitForTimeout(1500)
}

const marks = {}
if (mode.includes('harness')) {
  if (!traceFromLoad && !mode.includes('notrace')) await cdp.send('Tracing.start', { categories: cats, transferMode: 'ReportEvents' })
  if (process.env.SEARCH3) { for (let i = 0; i < 30 && (await page.locator('[data-recipe-name]').count()) < 36; i++) { await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight - innerHeight * 4)); await page.waitForTimeout(100); await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight)); await page.waitForTimeout(900) } await page.evaluate(() => window.scrollTo(0, 0)); await page.waitForTimeout(1500); result.cards = await page.locator('[data-recipe-name]').count() }
  marks.firstInput = await page.evaluate(() => performance.now())
  await page.mouse.move(phone ? 195 : 720, phone ? 422 : 450)
  await page.evaluate(() => window.__p.rec())
  await page.waitForTimeout(300)
  const distance = await page.evaluate(() => document.documentElement.scrollHeight - innerHeight)
  const steps = Math.min(Math.ceil(distance / 100), Number(process.env.STEPS ?? 1000))
  for (const dir of [1, -1]) for (let s = 0; s < steps; s++) { await page.mouse.wheel(0, dir * 100); await page.waitForTimeout(66) }
  await page.waitForTimeout(300)
  result.frames = await page.evaluate(() => { window.__p.on = false; return window.__p.frames.slice(1) })
  result.scrollEventsEnd = await page.evaluate(() => performance.now())
  if (process.env.HOVER !== '0') {
    const cards = page.locator('.kcc-slip--lift')
    const n = Math.min(6, await cards.count())
    for (let i = 0; i < n; i++) {
      const card = cards.nth(i)
      await card.scrollIntoViewIfNeeded()
      await page.waitForTimeout(400)
      const box = await card.boundingBox()
      if (!box) continue
      await page.mouse.move(2, 2)
      await page.waitForTimeout(250)
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 2 })
      await page.waitForTimeout(450)
    }
    await page.waitForTimeout(300)
  }
} else if (mode.includes('tap')) {
  if (!traceFromLoad && !mode.includes('notrace')) await cdp.send('Tracing.start', { categories: cats, transferMode: 'ReportEvents' })
  marks.firstInput = await page.evaluate(() => performance.now())
  const sel = process.env.TAP ?? 'header button'
  await page.locator(sel).first().click()
  await page.waitForTimeout(1500)
  marks.secondInput = await page.evaluate(() => performance.now())
  await page.keyboard.press('Escape')
  await page.waitForTimeout(1000)
}

if (traceFromLoad || !mode.includes('notrace')) {
  await cdp.send('Tracing.end')
  await done
}
const p = await page.evaluate(() => ({ events: window.__p.events, loaf: window.__p.loaf, shifts: window.__p.shifts, timeOrigin: performance.timeOrigin }))
Object.assign(result, marks, p)
writeFileSync(`${OUT}${label}.json`, JSON.stringify(result, null, 1))
if (events.length) writeFileSync(`${OUT}${label}.trace.json`, JSON.stringify({ traceEvents: events }))
const top = [...p.events].sort((a, b) => b.dur - a.dur).slice(0, 6)
console.log(label, 'hydrated', Math.round(hydrated), 'worst events', JSON.stringify(top.map((e) => [e.name, Math.round(e.start), e.dur, Math.round(e.ps - e.start), Math.round(e.pe - e.ps), e.target])))
console.log(label, 'loaf>50 after hydrated', JSON.stringify(p.loaf.filter((l) => l.dur > 50 && l.start > hydrated).slice(0, 8).map((l) => [Math.round(l.start), Math.round(l.dur), Math.round(l.block), l.scripts.slice(0, 3)])))
await browser.close()
