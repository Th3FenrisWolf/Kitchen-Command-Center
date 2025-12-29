import { fileURLToPath, URL } from 'node:url'

import { defineConfig, normalizePath, Plugin, UserConfig } from 'vite'
import { dirname, resolve } from 'path'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'
import fg from 'fast-glob'

// https://vite.dev/config/
export default defineConfig({
  build: {
    outDir: 'wwwroot',
  },
  plugins: [vue(), vueDevTools(), tailwindcss(), AutoEndpoints()],
  resolve: {
    alias: {
      '~': fileURLToPath(new URL('./Features', import.meta.url)),
    },
  },
})

// This plugin will find all files matching the pattern src/pages/*/App.vue
//  and make endpoints with the 'name' being the replacement for the *. and the
//  path being src/pages/*/main.ts - then return a template for the main.ts file
function AutoEndpoints(): Plugin {
  const main = `import '~/styles/main.css'

import { createApp } from 'vue'
import App from '{app}'

createApp(App).mount('#app')
`

  return {
    name: 'auto-endpoints',
    async config(): Promise<UserConfig> {
      const root = '../KitchenCommandCenter.Web/'
      const pattern = root + '*/App.vue'
      const length = root.length
      const dirs = (await fg.glob(pattern)).map((p) => dirname(p).substring(length))

      const input = dirs.reduce(
        (obj, item) => {
          const value = resolve(__dirname, root + item + '/main.ts')
          obj[item] = value
          return obj
        },
        {} as Record<string, string>,
      )

      return {
        build: {
          cssCodeSplit: true, // make the default explicit
          rollupOptions: {
            input: input,
          },
        },
      }
    },
    resolveId(id: string): null | string {
      return id.endsWith('main.ts') ? id : null
    },
    load(id: string): null | string {
      if (!id.endsWith('main.ts')) return null

      id = normalizePath(id)
      const app = id.replace(/.*\/src\//, '~/').replace('main.ts', 'App.vue')
      return main.replace('{app}', app)
    },
  }
}
