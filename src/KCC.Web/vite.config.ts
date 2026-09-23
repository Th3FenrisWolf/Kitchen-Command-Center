/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { execFileSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, renameSync, rmSync } from 'node:fs'

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

// Kestrel serves the channel domain over HTTPS, so a browser rejects dev-server assets fetched
// over plain HTTP as mixed content and the app never hydrates. Reusing ASP.NET Core's developer
// certificate keeps this origin trusted wherever the app's own origin already is.
function aspNetCoreDevCertificate() {
  const certificateDirectory = resolve(__dirname, 'node_modules/.dev-cert')
  const certificate = resolve(certificateDirectory, 'localhost.pem')
  const key = resolve(certificateDirectory, 'localhost.key')

  // dev:all runs the client and SSR dev servers as concurrent processes that both load this
  // config, so the export is staged per process and moved into place; sharing the destination
  // lets one export read what the other is still writing.
  if (!existsSync(certificate) || !existsSync(key)) {
    const staging = resolve(certificateDirectory, `staging-${process.pid}`)

    mkdirSync(staging, { recursive: true })
    execFileSync('dotnet', [
      'dev-certs',
      'https',
      '--export-path',
      resolve(staging, 'localhost.pem'),
      '--format',
      'PEM',
      '--no-password',
    ])
    renameSync(resolve(staging, 'localhost.pem'), certificate)
    renameSync(resolve(staging, 'localhost.key'), key)
    rmSync(staging, { recursive: true, force: true })
  }

  return { cert: readFileSync(certificate), key: readFileSync(key) }
}

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  const isSSR = mode === 'ssr'
  const servesBrowserAssets = command === 'serve' && !process.env.VITEST

  return {
    test: {
      environment: 'node',
      // Tests live in the repo-root /tests/KCC.ViteTests folder, not co-located under Features/.
      dir: resolve(__dirname, '../../tests/KCC.ViteTests'),
    },
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
      https: servesBrowserAssets ? aspNetCoreDevCertificate() : undefined,
      fs: {
        allow: [resolve(__dirname, '../..')],
      },
    },
    plugins: [
      vue(),
      tailwindcss(),
      ...(!isSSR
        ? [
            cleanAssetsPlugin(),
            // Razor emits the HTML, so the plugin's transformIndexHtml injection never
            // runs; appendTo injects the devtools client via the entry module instead.
            vueDevTools({ appendTo: 'Features/Main.ts' }),
          ]
        : []),
    ],
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
