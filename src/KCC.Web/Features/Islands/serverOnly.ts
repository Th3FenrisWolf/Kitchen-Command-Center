export {}

declare module 'vue' {
  interface ComponentCustomOptions {
    serverOnly?: boolean
  }
}
