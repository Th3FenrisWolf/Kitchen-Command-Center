import type { IslandEntry } from '~/Islands/payload'

export interface ContentRegions {
  headerContent: string
  bodyContent: string
  footerContent: string
}

export interface BloomInputs {
  seed: number
  page: string
}

export interface SsrPayload extends ContentRegions {
  isPreview: boolean
  bloom: BloomInputs
}

export interface IslandPayload {
  isPreview: boolean
  bloom: BloomInputs & { pageWashes: number }
  islands: IslandEntry[]
}
