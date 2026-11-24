/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { execFileSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, renameSync, rmSync } from 'node:fs'

import { defineConfig, normalizePath, type BuildOptions, type Plugin } from 'vite'
import { resolve } from 'path'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'
import { subsetFontAwesome } from './Features/Styles/FontAwesome/subsetFontAwesome'
import { inlineUnlessWashTexture } from './Features/Bloom/washTextures'

// Clean only the Vite assets folder before build, preserving fonts and other static files
function cleanAssetsPlugin(): Plugin {
  return {
    name: 'clean-assets',
    apply: 'build',
    buildStart() {
      const assetsDir = resolve(__dirname, 'wwwroot/assets')
      if (existsSync(assetsDir)) {
        rmSync(assetsDir, { recursive: true })
      }
    },
  }
}

const mainCss = normalizePath(resolve(__dirname, 'Features/Styles/Main.css'))

// Over HTTP/1.1 a page's chunk requests queue six at a time, each queue step a round trip, so every module the header
// imports that stays its own chunk delays the page view's chunk behind it. The header stays a chunk of its own: the
// manifest keys it by its component path, which IslandHeadLinks reads.
const headerModules =
  /Features\/Components\/(Header\/useRecentlyViewed\.ts|Account\/(SignOutForm\.vue|signOut\.ts)|Sheet\/(KccSheet\.vue|KccWashes\.Component\.vue))(\?.*)?$/

// Tailwind inlines Main.css's Font Awesome imports, so the subset runs on its output and before Vite's CSS pipeline.
function fontAwesomeSubsetPlugin(): Plugin {
  return {
    name: 'font-awesome-subset',
    enforce: 'pre',
    transform(code, id) {
      if (id.split('?')[0] !== mainCss) return
      return { code: subsetFontAwesome(code), map: null }
    },
  }
}

// Kestrel serves the site over HTTPS, so a browser rejects dev-server assets fetched
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

export default defineConfig(({ command, mode }) => {
  const isSSR = mode === 'ssr'
  const isFallback = mode === 'fallback'
  const servesBrowserAssets = command === 'serve' && !process.env.VITEST
  const isIslandBuild = command === 'build' && !isSSR && !isFallback

  const islandBuild = {
    outDir: 'wwwroot',
    emptyOutDir: false,
    manifest: true,
    sourcemap: 'hidden',
    // Base64 PNG barely gzips, so inlined into the stylesheet the wash textures cost more than they do as
    // cached files, which Layout.cshtml preloads.
    assetsInlineLimit: inlineUnlessWashTexture,
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'Features/Main.ts'),
        mainCss,
      },
      output: {
        codeSplitting: {
          groups: [{ name: 'HeaderImports', test: headerModules, minSize: 0, includeDependenciesRecursively: false }],
        },
      },
    },
  } satisfies BuildOptions

  // Vite.AspNetCore reads only wwwroot/.vite/manifest.json, so this build's own manifest is read by
  // Features/Islands/FallbackManifest.cs.
  const fallbackBuild = {
    outDir: 'wwwroot/fallback',
    emptyOutDir: true,
    manifest: true,
    sourcemap: 'hidden',
    assetsInlineLimit: inlineUnlessWashTexture,
    rollupOptions: {
      input: { fallback: resolve(__dirname, 'Features/Fallback.ts') },
    },
  } as const

  return {
    test: {
      environment: 'node',
      // Tests live in the repo-root /tests/KCC.ViteTests folder, not co-located under Features/.
      dir: resolve(__dirname, '../../tests/KCC.ViteTests'),
    },
    base: isFallback ? '/fallback/' : '/',
    publicDir: isFallback ? false : undefined,
    // The islands hydrate server HTML and compile no template in the browser, and no component uses the Options API.
    define: isIslandBuild
      ? {
          __VUE_OPTIONS_API__: false,
          __VUE_PROD_DEVTOOLS__: false,
          __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: false,
        }
      : undefined,
    ssr: { noExternal: ['vue'] },
    build: isSSR
      ? {
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
      : isFallback
        ? fallbackBuild
        : islandBuild,
    server: {
      https: servesBrowserAssets ? aspNetCoreDevCertificate() : undefined,
      fs: {
        allow: [resolve(__dirname, '../..')],
      },
    },
    plugins: [
      vue(),
      tailwindcss(),
      fontAwesomeSubsetPlugin(),
      ...(!isSSR && !isFallback
        ? [
            cleanAssetsPlugin(),
            // Razor emits the HTML, so the plugin's transformIndexHtml injection never
            // runs; appendTo injects the devtools client via the entry module instead.
            vueDevTools({ appendTo: 'Features/Main.ts' }),
          ]
        : []),
    ],
    resolve: {
      // Vite resolves a deduped package from this folder only, and pnpm links Vue's own @vue/* dependencies beside Vue
      // in its store, out of reach from here. Deduped, the dev SSR runner inlined their CommonJS builds and failed.
      dedupe: ['vue'],
      alias: [
        // App.vue renders the region templates with the template compiler, which the SSR render, the fallback and
        // the dev server need. The island build resolves vue to the package default, the runtime-only build.
        // Exact match to avoid catching vue/* subpaths.
        ...(isIslandBuild ? [] : [{ find: /^vue$/, replacement: 'vue/dist/vue.esm-bundler.js' }]),
        { find: '~', replacement: fileURLToPath(new URL('./Features', import.meta.url)) },
      ],
    },
  }
})
