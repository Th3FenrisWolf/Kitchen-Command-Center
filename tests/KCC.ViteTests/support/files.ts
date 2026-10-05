import { readdirSync } from 'node:fs'
import { join } from 'node:path'

export function* files(dir: string, name: RegExp): Generator<string> {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) yield* files(path, name)
    else if (name.test(entry.name)) yield path
  }
}
