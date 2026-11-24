import { readFileSync } from 'node:fs'
const [file, fromMs, toMs, minDur = '8'] = process.argv.slice(2)
const ev = JSON.parse(readFileSync(file)).traceEvents
const names = {}
for (const e of ev) if (e.ph === 'M' && e.name === 'thread_name') names[e.pid + ':' + e.tid] = e.args.name
const a = ev.find((e) => e.name === 'EventTiming' && e.ph === 'b')
const ts0 = a.ts - a.args.data.timeStamp * 1000
const from = ts0 + fromMs * 1000, to = ts0 + toMs * 1000
const open = {}
const rows = []
for (const e of ev) {
  if (e.ph === 'X' && e.dur >= minDur * 1000 && e.ts < to && e.ts + e.dur > from) rows.push([((e.ts - ts0) / 1000).toFixed(1), (e.dur / 1000).toFixed(1), names[e.pid + ':' + e.tid] ?? e.tid, e.name, JSON.stringify(e.args?.data ?? e.args ?? {}).slice(0, 140)])
  if (e.ph === 'B') (open[e.pid + e.tid + e.name] ??= []).push(e)
  if (e.ph === 'E') { const b = open[e.pid + e.tid + e.name]?.pop(); if (b && e.ts - b.ts >= minDur * 1000 && b.ts < to && e.ts > from) rows.push([((b.ts - ts0) / 1000).toFixed(1), ((e.ts - b.ts) / 1000).toFixed(1), names[e.pid + ':' + e.tid] ?? e.tid, e.name, '']) }
}
rows.sort((x, y) => x[0] - y[0])
for (const r of rows) console.log(r.join(' | '))
