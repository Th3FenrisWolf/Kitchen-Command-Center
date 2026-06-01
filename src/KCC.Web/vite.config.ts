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
    apply: 'build',
    buildStart() {
      // Preserve fonts and other static files
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
    ssr: { noExternal: ['vue'] },
    build: isSSR
      ? {
          // SSR build configuration
          ssr: true,
          outDir: 'wwwroot/ssr',
          emptyOutDir: true,
          minify: 'esbuild',
          rollupOptions: {
            input: resolve(__dirname, 'Features/Ssr/Server.Entry.ts'),
            output: {
              entryFileNames: 'Server.Entry.js',
            },
          },
        }
      : {
          // Client build configuration
          outDir: 'wwwroot',
          emptyOutDir: false,
          manifest: true,
          sourcemap: 'hidden',
          rollupOptions: {
            input: {
              main: resolve(__dirname, 'Features/Main.ts'),
              mainCss: resolve(__dirname, 'Features/Styles/Main.css'),
              pageBuilderMount: resolve(__dirname, 'Features/PageBuilderMount.ts'),
              adminHomeRedirect: resolve(__dirname, 'Features/AdminHomePage/admin-home-redirect.ts'),
              resourceStringEditor: resolve(
                __dirname,
                '../KCC.ResourceStrings/Client/src/resource-strings/ResourceStringEditor.ts',
              ),
            },
          },
        },
    server: {
      fs: {
        allow: [resolve(__dirname, '..')],
      },
    },
    plugins: [vue(), tailwindcss(), ...(!isSSR ? [cleanAssetsPlugin(), vueDevTools()] : [])],
    resolve: {
      dedupe: ['vue', '@vue/runtime-dom', '@vue/runtime-core', '@vue/compiler-dom', '@vue/shared'],
      alias: [
        // Use runtime compiler build for dynamic template strings
        // Exact match to avoid catching vue/* subpaths
        { find: /^vue$/, replacement: 'vue/dist/vue.esm-bundler.js' },
        { find: '~', replacement: fileURLToPath(new URL('./Features', import.meta.url)) },
      ],
    },
  }
})
