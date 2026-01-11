import { fileURLToPath, URL } from 'node:url'
import { existsSync, rmSync } from 'node:fs'

import { defineConfig, type Plugin } from 'vite'
import { resolve } from 'path'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// Clean only the Vite assets folder before build, preserving fonts and other static files
function cleanAssetsPlugin(): Plugin {
  return {
    name: 'clean-assets',
    apply: 'build', // Only run during production builds, not dev server
    buildStart() {
      const assetsDir = resolve(__dirname, 'wwwroot/assets')
      if (existsSync(assetsDir)) {
        rmSync(assetsDir, { recursive: true })
      }
    },
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const isSSR = mode === 'ssr'

  return {
    build: isSSR
      ? {
          // SSR build configuration
          ssr: true,
          outDir: 'wwwroot/ssr',
          emptyOutDir: true, // Safe to clear - only contains build output
          minify: 'esbuild',
          rollupOptions: {
            input: resolve(__dirname, 'Features/Ssr/entry-server.ts'),
            output: {
              entryFileNames: 'entry-server.js',
            },
          },
        }
      : {
          // Client build configuration
          outDir: 'wwwroot',
          emptyOutDir: false, // Preserve fonts and other static files
          manifest: true, // Generate manifest.json for Vite.AspNetCore
          rollupOptions: {
            input: {
              main: resolve(__dirname, 'Features/main.ts'),
            },
          },
        },
    plugins: [
      // Clean old Vite assets before client build (not needed for SSR)
      ...(!isSSR ? [cleanAssetsPlugin()] : []),
      vue(),
      // Only include devtools for client dev mode
      ...(!isSSR ? [vueDevTools()] : []),
      tailwindcss(),
    ],
    resolve: {
      // Dedupe Vue packages to prevent duplicate runtime compiler code
      dedupe: ['vue', '@vue/runtime-dom', '@vue/runtime-core', '@vue/compiler-dom', '@vue/shared'],
      alias: [
        // Features path alias
        { find: '~', replacement: fileURLToPath(new URL('./Features', import.meta.url)) },
        // Use runtime compiler build for dynamic template strings
        // Exact match to avoid catching vue/* subpaths
        { find: /^vue$/, replacement: 'vue/dist/vue.esm-bundler.js' },
      ],
    },
    ssr: {
      // Don't externalize vue - bundle it for SSR
      noExternal: ['vue'],
    },
  }
})
