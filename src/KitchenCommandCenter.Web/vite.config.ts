import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import { resolve } from 'path'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  build: {
    outDir: 'wwwroot',
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'Features/main.ts'),
      },
    },
  },
  plugins: [vue(), vueDevTools(), tailwindcss()],
  resolve: {
    alias: [
      // Features path alias
      { find: '~', replacement: fileURLToPath(new URL('./Features', import.meta.url)) },
      // Use runtime compiler build for dynamic template strings
      // Exact match to avoid catching vue/* subpaths
      { find: /^vue$/, replacement: 'vue/dist/vue.esm-bundler.js' },
    ],
  },
})
