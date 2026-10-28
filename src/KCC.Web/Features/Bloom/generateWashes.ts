// Writes Styles/Torn/Washes.css and its textures. Run from src/KCC.Web with `yarn washes`.
import { mkdirSync, readdirSync, rmSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { bakeWashes, washTextures, washesCss } from './bakeWashes.ts'
import { WASH_TEXTURE_FOLDER } from './washTextures.ts'

const target = fileURLToPath(new URL('../Styles/Torn/Washes.css', import.meta.url))
const folder = fileURLToPath(new URL(`../Styles/Torn/${WASH_TEXTURE_FOLDER}/`, import.meta.url))
const baked = bakeWashes()
const textures = washTextures(baked)

mkdirSync(folder, { recursive: true })
for (const stale of readdirSync(folder)) if (!textures.some(({ file }) => file === stale)) rmSync(folder + stale)
for (const { file, png } of textures) writeFileSync(folder + file, png)
writeFileSync(target, washesCss(baked))
process.stdout.write(`wrote ${target} and ${textures.length} textures in ${folder}\n`)
