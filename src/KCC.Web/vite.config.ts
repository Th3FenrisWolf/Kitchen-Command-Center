import { fileURLToPath, URL } from 'node:url'
import { existsSync, mkdirSync, rmSync, copyFileSync } from 'node:fs'

import { defineConfig, type Plugin } from 'vite'
import { resolve, dirname } from 'path'
import { build as esbuild } from 'esbuild'
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

// Compile inline editor TS/CSS from Features/ to wwwroot/PageBuilder/Admin/
// where Kentico's <page-builder-scripts /> auto-discovers them.
const pageBuilderAssets: Record<string, { src: string; dest: string; format?: 'iife' | 'esm' }> = {
  bentoBoxEditor: {
    src: 'Features/Sections/BentoBox/bento-box-editor.ts',
    dest: 'wwwroot/PageBuilder/Admin/InlineEditors/BentoBoxEditor/bento-box-editor.js',
    format: 'iife',
  },
  bentoBoxEditorCss: {
    src: 'Features/Sections/BentoBox/bento-box-editor.css',
    dest: 'wwwroot/PageBuilder/Admin/InlineEditors/BentoBoxEditor/bento-box-editor.css',
  },
}

function pageBuilderScriptsPlugin(): Plugin {
  return {
    name: 'page-builder-scripts',
    apply: 'build',
    async closeBundle() {
      for (const [name, entry] of Object.entries(pageBuilderAssets)) {
        const srcPath = resolve(__dirname, entry.src)
        const destPath = resolve(__dirname, entry.dest)

        if (!existsSync(srcPath)) {
          console.warn(`[page-builder-scripts] Source not found: ${entry.src}`)
          continue
        }

        const destDir = dirname(destPath)
        if (!existsSync(destDir)) {
          mkdirSync(destDir, { recursive: true })
        }

        if (entry.src.endsWith('.css')) {
          copyFileSync(srcPath, destPath)
          console.log(`[page-builder-scripts] Copied ${name}: ${entry.dest}`)
        } else {
          await esbuild({
            entryPoints: [srcPath],
            outfile: destPath,
            bundle: true,
            format: entry.format ?? 'iife',
            minify: true,
          })
          console.log(`[page-builder-scripts] Built ${name}: ${entry.dest}`)
        }
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
    plugins: [vue(), tailwindcss(), ...(!isSSR ? [cleanAssetsPlugin(), pageBuilderScriptsPlugin(), vueDevTools()] : [])],
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
