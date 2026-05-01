# KitchenCommandCenter

A Kentico Xperience application with Vue 3 server-side rendering (SSR).

## Table of Contents

- [Local Development](#local-development)
- [Architecture](#architecture)
- [Docker Deployment](#docker-deployment)
- [Common Commands](#common-commands)

---

## Local Development

### Development Environment Setup

**Working Directory**: All commands should be run from `./src/KCC.Web`

```bash
cd src/KCC.Web
```

### Quick Development Workflow

1. **Restore Tools**:

   ```bash
   dotnet tool restore
   ```

2. **Database Setup**:
   ```bash
   dotnet kentico-xperience-dbmanager -- -s "localhost" -d "YourDatabase" -a "AdminPassword" --hash-string-salt "HashStringSaltForSolution"
   ```

> Note: Regardless of what `AdminPassword` is, it will be overwritten once Kentico CI is restored to be the default admin credentials found in 1pass.

3. **Configure Secrets**:
   - Copy connection string and hash salt from `appsettings.json` to `secrets.json`
   - Run: `type secrets.json | dotnet user-secrets set`
   - Remove sensitive values from `appsettings.json`

4. **Restore Database Content**:

   ```bash
   dotnet run --kxp-ci-restore
   ```

5. **Install Frontend Dependencies** (from `src/KCC.Web`):

   ```bash
   yarn install
   ```

6. **Run the Application**:

   The application requires three services running concurrently:
   - ASP.NET Core web server
   - Vite dev server (for HMR)
   - Vue SSR service

   ```bash
   dotnet watch
   ```

   This automatically runs `yarn dev:all` which starts Vite, SSR, and CSS watchers alongside the .NET app.

The root URL will be the live site's home page. To access the administration interface, navigate to the `/admin` path. Admin credentials can be found in 1pass to create your own account.

---

## Architecture

This starter follows established patterns and best practices for maintainable, scalable Xperience applications.

### Vertical Slice Architecture

Features are organized by vertical slices in `src/KCC.Web/Features/`:

```
Features/
├── Widgets/
│   └── Accordion/
│       ├── AccordionViewComponent.cs
│       ├── AccordionViewModel.cs
│       ├── AccordionProperties.cs
│       └── Accordion.cshtml
└── Pages/
    └── HomePage/
        ├── HomePageController.cs
        ├── HomePageViewModel.cs
        └── HomePage.cshtml
```

Each feature contains all related files (controllers, view models, views) in one location.

### Content Modeling Standards

#### Field Naming Conventions

Use consistent field names across content types and widgets:

- **Heading**: Primary title text
- **SubHeading**: Supporting title text
- **Body**: Main content/description

#### Field Ordering

1. **Primary Function**: Core functionality (e.g., item selectors for listing widgets)
2. **Content Fields**: Headings, text, media
3. **Styling Options**: Colors, spacing, layout options

### Development Best Practices

#### Caching Strategy

- **Output Caching**: Applied to all widgets and pages with proper cache dependencies
- **Data Caching**: Use `CacheService` wrapper for database queries

#### Widget Guidelines

- **Never Render Empty**: Always show configuration prompts when setup is needed
- **Use Constants**: Leverage `WidgetConstants.ConfigHeading` and `WidgetConstants.ConfigSubHeading`
- **Text Editing**: Enable inline editing for all text content where possible

---

## Docker Deployment

The application uses Docker Compose to orchestrate two services:

| Service | Description              | Port            |
| ------- | ------------------------ | --------------- |
| `web`   | ASP.NET Core application | 8080 (exposed)  |
| `ssr`   | Vue SSR Node.js service  | 3001 (internal) |

### Prerequisites

- Docker Desktop or Docker Engine with Compose v2
- Access to your SQL Server database from the Docker network

### Quick Start

1. **Create environment file**:

   ```bash
   cp .env.example .env
   ```

2. **Configure `.env`** with your database connection:

   ```env
   CMS_CONNECTION_STRING=Server=host.docker.internal;Database=KitchenCommandCenter;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
   CMS_HASH_STRING_SALT=your-hash-salt-from-appsettings
   ```

   > Note: Use `host.docker.internal` to connect to a database running on your host machine.

3. **Build and run**:

   ```bash
   # Development
   docker compose up --build

   # Production
   .\deploy.ps1 up -d --build
   ```

4. **Access the application** at http://localhost:8080

### Production Deployment

Use the production compose file for hardened settings:

```bash
# Using the deploy script (recommended)
.\deploy.ps1 up -d --build

# Or manually
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Production settings include:

- Automatic restart on failure
- Memory limits (1GB for web, 512MB for SSR)
- Log rotation
- Read-only filesystem
- Security hardening

### Common Docker Commands

```bash
# View logs
.\deploy.ps1 logs -f

# View logs for specific service
.\deploy.ps1 logs -f web

# Stop all services
.\deploy.ps1 down

# Restart a service
.\deploy.ps1 restart web

# Rebuild and restart
.\deploy.ps1 up -d --build

# Check service health
.\deploy.ps1 ps
```

### Architecture Diagram

```
┌──────────────────────────────────────────────────────────┐
│                    Docker Network                         │
│                                                          │
│   ┌─────────────────┐        ┌─────────────────────────┐│
│   │                 │  HTTP  │                         ││
│   │  web:8080       │───────▶│  ssr:3001 (internal)    ││
│   │  (ASP.NET Core) │        │  (Node.js/Express)      ││
│   │                 │        │                         ││
│   └────────┬────────┘        └─────────────────────────┘│
│            │                                             │
└────────────┼─────────────────────────────────────────────┘
             │
             ▼
        localhost:8080
```

### Troubleshooting

**SSR service unhealthy**:

```bash
# Check SSR logs
.\deploy.ps1 logs ssr

# Restart SSR service
.\deploy.ps1 restart ssr
```

**Database connection issues**:

- Ensure SQL Server allows remote connections
- For local SQL Server, use `host.docker.internal` as the server name
- Check firewall rules allow connections on port 1433

**Build failures**:

```bash
# Rebuild without cache
docker compose build --no-cache
```

---

## Common Commands

Essential CLI commands for day-to-day development.

### Code Generation

Generate strongly-typed classes for content types:

```bash
dotnet run --no-build -- --kxp-codegen --type "PageContentTypes" --location ".\Models\Generated\{type}\{dataClassNamespace}"
```

**Available Types:**

- `Forms`
- `ReusableContentTypes`
- `PageContentTypes`
- `ReusableFieldSchemas`
- `Classes`

### Kentico Upgrades

1. **Update Packages**:

   ```bash
   dotnet add package Kentico.Xperience.WebApp
   # Repeat for other Kentico packages
   ```

2. **Upgrade Database**:
   ```bash
   dotnet run --no-build --kxp-update
   ```

### CI/CD Operations

#### Continuous Integration

**Store Changes**:

```bash
dotnet run --no-build --kxp-ci-store
```

**Restore Changes** (after pulling branch changes):

```bash
dotnet run --no-build --kxp-ci-restore
```

#### Continuous Deployment

**Create CD Configuration**:

```bash
dotnet run --no-build -- --kxp-cd-config --path ".\App_Data\CDRepository\repository.config"
```

**Store Repository**:

```bash
dotnet run --no-build -- --kxp-cd-store --repository-path ".\App_Data\CDRepository"
```

**Restore Repository**:

```bash
dotnet run --no-build -- --kxp-cd-restore --repository-path ".\App_Data\CDRepository"
```

---

## Frontend Development

### IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (disable Vetur if installed).

### Frontend Commands

Run from `src/KCC.Web`:

```bash
# Install dependencies
yarn

# Development (Vite + SSR + CSS watching)
yarn dev:all

# Type checking
yarn type-check

# Linting
yarn lint

# Build for production (client + SSR bundles)
yarn build:all
```

### Vue SSR

The application uses Vue 3 server-side rendering. The SSR service:

- Runs on port 3001 in development
- Pre-renders Vue components on the server for better SEO and initial load
- Falls back gracefully to client-side rendering if SSR is unavailable

See [Vite Configuration Reference](https://vite.dev/config/) for build customization.
