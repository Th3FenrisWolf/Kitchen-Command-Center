import express from 'express'
import compression from 'compression'
import { createServer as createHttpServer } from 'http'
import { randomUUID } from 'crypto'
import { renderToString } from 'vue/server-renderer'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const SSR_BUNDLE_PATH = resolve(__dirname, '../../wwwroot/ssr/Server.Entry.js')
const SSR_ENTRY_PATH = resolve(__dirname, 'Server.Entry.ts')

const PORT = process.env.SSR_PORT || 3001
const isDev = process.env.NODE_ENV !== 'production'

// Simple logger that respects environment
const log = {
  /* eslint-disable no-console */
  info: (...args) => isDev && console.log(...args),
  error: (...args) => console.error(...args),
  debug: (...args) => isDev && console.debug(...args),
  /* eslint-enable no-console */
}

// Start true to trigger initial load
let moduleInvalidated = true
let createApp
let vite

// Metrics for health check
const metrics = {
  startTime: Date.now(),
  renderCount: 0,
  renderErrors: 0,
  lastRenderTime: null,
}

async function loadModule() {
  if (isDev && vite) {
    // In dev, use Vite's ssrLoadModule for proper HMR
    const ssrModule = await vite.ssrLoadModule(SSR_ENTRY_PATH)
    createApp = ssrModule.createApp
    moduleInvalidated = false
    log.info('SSR module loaded via Vite dev server')
  } else {
    // In production, use the pre-built bundle
    const ssrModule = await import(SSR_BUNDLE_PATH)
    createApp = ssrModule.createApp
    log.info('SSR bundle loaded')
  }
}

async function createServer() {
  const app = express()
  const httpServer = createHttpServer(app)

  // In development, create Vite dev server in middleware mode for SSR
  if (isDev) {
    const { createServer: createViteServer } = await import('vite')
    vite = await createViteServer({
      server: {
        middlewareMode: true,
        hmr: {
          server: httpServer,
        },
      },
      appType: 'custom',
    })
    app.use(vite.middlewares)

    // Listen for HMR updates to invalidate the cached module
    vite.watcher.on('change', (file) => {
      if (file.endsWith('.ts') || file.endsWith('.vue') || file.endsWith('.css')) {
        moduleInvalidated = true
        log.debug(`File changed: ${file}, SSR module marked for reload`)
      }
    })

    log.info('Vite dev server initialized in middleware mode')
  }

  app.use(compression())
  app.use(express.json({ limit: '10mb' }))

  app.use((req, res, next) => {
    req.id = randomUUID().slice(0, 8)
    next()
  })

  // Load the SSR module
  try {
    await loadModule()
  } catch (err) {
    log.error('✗ Failed to load SSR module:', err.message)
    if (!isDev) {
      log.error('Run "yarn build:ssr" first.')
      process.exit(1)
    }
  }

  app.get('/health', (_, res) => {
    const memUsage = process.memoryUsage()
    res.json({
      status: 'ok',
      timestamp: new Date().toISOString(),
      mode: isDev ? 'development' : 'production',
      uptime: Math.floor((Date.now() - metrics.startTime) / 1000),
      memory: {
        heapUsed: Math.round(memUsage.heapUsed / 1024 / 1024),
        heapTotal: Math.round(memUsage.heapTotal / 1024 / 1024),
        rss: Math.round(memUsage.rss / 1024 / 1024),
      },
      renders: {
        total: metrics.renderCount,
        errors: metrics.renderErrors,
        lastRenderTime: metrics.lastRenderTime,
      },
    })
  })

  app.post('/render', async (req, res) => {
    const startTime = Date.now()

    try {
      const { headerContent, bodyContent, footerContent } = req.body

      if (
        typeof headerContent !== 'string' ||
        typeof bodyContent !== 'string' ||
        typeof footerContent !== 'string'
      ) {
        return res.status(400).json({
          error: 'headerContent, bodyContent, and footerContent must be strings',
        })
      }

      log.debug(`[${req.id}] SSR render started`)

      // In dev mode, only reload when module has been invalidated by file changes
      if (isDev && vite && moduleInvalidated) {
        await loadModule()
      }

      const ssrApp = createApp({ headerContent, bodyContent, footerContent })
      const html = await renderToString(ssrApp)

      const duration = Date.now() - startTime
      log.debug(`[${req.id}] SSR render completed in ${duration}ms`)

      // Update metrics
      metrics.renderCount++
      metrics.lastRenderTime = duration

      res.json({ html, renderTime: duration })
    } catch (err) {
      const duration = Date.now() - startTime
      log.error(`[${req.id}] SSR render failed after ${duration}ms:`, err.message)

      // Update error metrics
      metrics.renderErrors++

      if (isDev && vite) {
        vite.ssrFixStacktrace(err)
      }

      res.status(500).json({
        error: err.message,
        stack: isDev ? err.stack : undefined,
      })
    }
  })

  httpServer.listen(PORT, () => {
    log.info(`
╔══════════════════════════════════════════════╗
║   Vue SSR Service                            ║
║   Running on http://localhost:${PORT}           ║
║   Environment: ${(isDev ? 'Development' : 'Production').padEnd(30)}║
╚══════════════════════════════════════════════╝
    `)
  })

  const shutdown = async (signal) => {
    log.info(`\n${signal} received. Shutting down.`)

    if (vite) await vite.close()

    httpServer.close(() => {
      log.info('SSR service stopped.')
      process.exit(0)
    })

    setTimeout(() => process.exit(1), 10000)
  }

  process.on('SIGTERM', () => shutdown('SIGTERM'))
  process.on('SIGINT', () => shutdown('SIGINT'))
}

createServer()
