// Writes Styles/Torn/Tears.css. Run from src/KCC.Web with `yarn tears`.
import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { tearsCss } from './tears.ts'

const target = fileURLToPath(new URL('../Styles/Torn/Tears.css', import.meta.url))
mkdirSync(dirname(target), { recursive: true })
writeFileSync(target, tearsCss())
process.stdout.write(`wrote ${target}\n`)
