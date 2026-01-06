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
    alias: {
      '~': fileURLToPath(new URL('./Features', import.meta.url)),
      vue: 'vue/dist/vue.esm-bundler.js', // Use runtime compiler build
    },
  },
})
