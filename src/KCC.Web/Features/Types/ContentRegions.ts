export interface ContentRegions {
  headerContent: string
  bodyContent: string
  footerContent: string
}

export interface SsrPayload extends ContentRegions {
  isPreview: boolean
}
