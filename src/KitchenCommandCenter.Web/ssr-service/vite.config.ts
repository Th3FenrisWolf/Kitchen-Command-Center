import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import { resolve } from 'path'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// SSR build configuration
export default defineConfig({
  build: {
    ssr: true,
    outDir: 'dist',
    // Use production mode for smaller bundles
    minify: 'esbuild',
    rollupOptions: {
      input: resolve(__dirname, 'entry-server.ts'),
      output: {
        entryFileNames: 'entry-server.js',
      },
    },
  },
  plugins: [
    vue({
      // Use runtime compiler for dynamic template strings from server
      template: {
        compilerOptions: {
          // Allow custom elements if needed
        },
      },
    }),
    tailwindcss(),
  ],
  resolve: {
    // Dedupe Vue packages to prevent duplicate runtime compiler code
    dedupe: ['vue', '@vue/runtime-dom', '@vue/runtime-core', '@vue/compiler-dom', '@vue/shared'],
    alias: [
      // Features path alias
      { find: '~', replacement: fileURLToPath(new URL('../Features', import.meta.url)) },
      // Use the full Vue build with runtime compiler for SSR
      // This allows rendering dynamic template strings from server content
      // Use exact match ($) to avoid catching vue/server-renderer
      { find: /^vue$/, replacement: 'vue/dist/vue.esm-bundler.js' },
    ],
  },
  ssr: {
    // Don't externalize vue - bundle it for SSR
    noExternal: ['vue'],
  },
})
