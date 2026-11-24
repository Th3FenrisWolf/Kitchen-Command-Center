export interface IslandEntry {
  name: string
  props: Record<string, unknown>
  keys: string[]
}

export interface ServerIslands {
  islands: IslandEntry[]
  components: string[]
}

export interface RenderOutput extends ServerIslands {
  pageWashes: number
}
