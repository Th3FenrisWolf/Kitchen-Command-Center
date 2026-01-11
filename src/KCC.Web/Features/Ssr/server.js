/* eslint-disable no-console */

import express from 'express'
import compression from 'compression'
import { randomUUID } from 'crypto'
import { renderToString } from 'vue/server-renderer'

const PORT = process.env.SSR_PORT || 3001
const isDev = process.env.NODE_ENV !== 'production'

// Simple logger that respects environment
const log = {
  info: (...args) => isDev && console.log(...args),
  error: (...args) => console.error(...args),
  debug: (...args) => isDev && console.debug(...args),
}

async function createServer() {
  const server = express()

  // Enable compression for responses
  server.use(compression())

  // Parse JSON with size limit
  server.use(express.json({ limit: '10mb' }))

  // Request ID middleware for tracing
  server.use((req, res, next) => {
    req.id = randomUUID().slice(0, 8)
    next()
  })

  // Import the SSR bundle
  let createApp
  try {
    const ssrModule = await import('../../wwwroot/ssr/entry-server.js')
    createApp = ssrModule.createApp
    log.info('✓ SSR bundle loaded successfully')
  } catch (err) {
    log.error('✗ Failed to load SSR bundle. Run "npm run build" first.')
    log.error(err)
    process.exit(1)
  }

  // Root endpoint - helpful info
  server.get('/', (_, res) => {
    res.send(`
      <html>
        <head><title>Vue SSR Service</title></head>
        <body style="font-family: system-ui; padding: 2rem;">
          <h1>🚀 Vue SSR Service</h1>
          <p>This service handles server-side rendering for Vue components.</p>
          <h2>Endpoints:</h2>
          <ul>
            <li><code>GET /health</code> - Health check</li>
            <li><code>POST /render</code> - Render Vue app to HTML</li>
          </ul>
          <p>Status: <strong style="color: green;">Running</strong></p>
          <p>Environment: <strong>${isDev ? 'Development' : 'Production'}</strong></p>
        </body>
      </html>
    `)
  })

  // Health check endpoint
  server.get('/health', (_, res) => {
    res.json({ status: 'ok', timestamp: new Date().toISOString() })
  })

  // SSR render endpoint
  server.post('/render', async (req, res) => {
    const startTime = Date.now()

    try {
      const { headerContent, bodyContent, footerContent } = req.body

      if (typeof headerContent !== 'string' || typeof bodyContent !== 'string' || typeof footerContent !== 'string') {
        return res.status(400).json({
          error: 'headerContent, bodyContent, and footerContent must be strings',
        })
      }

      log.debug(`[${req.id}] SSR render started`)

      const app = createApp({ headerContent, bodyContent, footerContent })
      const html = await renderToString(app)

      const duration = Date.now() - startTime
      log.debug(`[${req.id}] SSR render completed in ${duration}ms`)

      res.json({ html, renderTime: duration })
    } catch (err) {
      const duration = Date.now() - startTime
      log.error(`[${req.id}] SSR render failed after ${duration}ms:`, err.message)

      res.status(500).json({
        error: err.message,
        stack: isDev ? err.stack : undefined,
      })
    }
  })

  // Error handling middleware for malformed JSON and other errors
  server.use((err, req, res) => {
    if (err instanceof SyntaxError && 'body' in err) {
      log.error(`[${req.id}] Invalid JSON in request body`)
      return res.status(400).json({ error: 'Invalid JSON in request body' })
    }

    log.error(`[${req.id}] Unhandled error:`, err.message)
    res.status(500).json({
      error: 'Internal server error',
      message: isDev ? err.message : undefined,
    })
  })

  // Start server
  const serverInstance = server.listen(PORT, () => {
    console.log(`
╔══════════════════════════════════════════════╗
║   Vue SSR Service                            ║
║   Running on http://localhost:${PORT}           ║
║   Environment: ${(isDev ? 'Development' : 'Production').padEnd(30)}║
╚══════════════════════════════════════════════╝
    `)
  })

  // Process shutdown handling
  const shutdown = (signal) => {
    console.log(`\n${signal} received. Shutting down.`)

    serverInstance.close((err) => {
      if (err) {
        log.error('Error during shutdown:', err)
        process.exit(1)
      }
      console.log('SSR service stopped.')
      process.exit(0)
    })

    // Force exit after 10 seconds if process shutdown fails
    setTimeout(() => {
      log.error('Could not close connections in time, forcefully shutting down')
      process.exit(1)
    }, 10000)
  }

  process.on('SIGTERM', () => shutdown('SIGTERM'))
  process.on('SIGINT', () => shutdown('SIGINT'))
}

createServer()
