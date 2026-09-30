import { defineConfig, type Plugin, type UserConfig } from 'vite'

export interface BackofficeClient {
  packageName: string
  title: string
}

// Umbraco only cache-busts an App_Plugins entry by the package version, so the entry file name carries a content
// hash instead, and the manifest that points at it is written once the bundle's file names are known.
function umbracoPackage({ packageName, title }: BackofficeClient): Plugin {
  return {
    name: 'kcc-umbraco-package',
    generateBundle(_, bundle) {
      const entry = Object.values(bundle).find((file) => file.type === 'chunk' && file.isEntry)
      if (!entry) {
        this.error('The build produced no entry chunk.')
      }

      this.emitFile({
        type: 'asset',
        fileName: 'umbraco-package.json',
        source: JSON.stringify(
          {
            id: packageName,
            name: title,
            allowTelemetry: false,
            extensions: [
              {
                type: 'bundle',
                alias: `${packageName}.Bundle`,
                name: `${title} Bundle`,
                js: `/App_Plugins/${packageName}/${entry.fileName}`,
              },
            ],
          },
          null,
          2,
        ),
      })
    },
  }
}

// Not a library build: library mode inlines every asset, and the icon font alone would add 470 kB to a chunk.
export function backofficeClient(client: BackofficeClient): UserConfig {
  return defineConfig({
    base: `/App_Plugins/${client.packageName}/`,
    build: {
      outDir: `../wwwroot/App_Plugins/${client.packageName}`,
      emptyOutDir: true,
      sourcemap: true,
      rolldownOptions: {
        input: 'src/bundle.ts',
        preserveEntrySignatures: 'exports-only',
        external: [/^@umbraco/],
        output: {
          entryFileNames: 'bundle-[hash].js',
          chunkFileNames: '[name]-[hash].js',
          assetFileNames: '[name]-[hash][extname]',
        },
      },
    },
    plugins: [umbracoPackage(client)],
  })
}
