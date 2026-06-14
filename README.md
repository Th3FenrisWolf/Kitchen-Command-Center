# KitchenCommandCenter

A Kentico Xperience application with Vue 3 server-side rendering (SSR).

## Table of Contents

- [Local Development](#local-development)
- [Architecture](#architecture)
- [Deployment](#deployment)
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

   > Requires a **Font Awesome Pro** token — set `FONTAWESOME_NPM_AUTH_TOKEN` first (see [Font Awesome Pro](#font-awesome-pro)). The admin client in `src/KCC.Admin/Client` needs `yarn install` with the same token, too.

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

### Font Awesome Pro

This project renders icons with **Font Awesome Pro** (webfont / CSS), installed from Font Awesome's private npm registry. The registry is configured in committed `.npmrc` files (`src/KCC.Web/.npmrc` and `src/KCC.Admin/Client/.npmrc`); the auth token is **not** committed — it is read from the `FONTAWESOME_NPM_AUTH_TOKEN` environment variable.

Before running `yarn install` in **either** `src/KCC.Web` or `src/KCC.Admin/Client`:

1. Get a token from your Font Awesome account (Account → Tokens).
2. Set `FONTAWESOME_NPM_AUTH_TOKEN` in your environment:
   - PowerShell (persists; reopen the terminal afterward): `setx FONTAWESOME_NPM_AUTH_TOKEN "<token>"`
   - PowerShell (current session only): `$env:FONTAWESOME_NPM_AUTH_TOKEN = "<token>"`
   - bash / zsh: `export FONTAWESOME_NPM_AUTH_TOKEN=<token>`

The same variable must be set wherever the production frontend is built. Never commit the token or any Font Awesome font files — `node_modules/`, `**/wwwroot/assets`, `**/wwwroot/webfonts`, and `src/KCC.Admin/Client/dist/` are all git-ignored.

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
