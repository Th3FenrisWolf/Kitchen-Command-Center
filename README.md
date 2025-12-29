# KitchenCommandCenter

## Local Development

### Development Environment Setup

**Working Directory**: All commands should be run from `./src/KitchenCommandCenter.Web`

```bash
cd src/KitchenCommandCenter.Web
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

5. **Build Client Assets**:

   ```bash
   cd client-ui
   yarn && yarn build
   cd ..
   ```

6. **Run the Application**:

   ```bash
   # With hot reload
   dotnet watch

   # Standard run
   dotnet run
   ```

The root URL will be the live site's home page. To access the administration interface, navigate to the `/admin` path. Admin credentials can be found in 1pass to create your own account

---

## Architecture

This starter follows established patterns and best practices for maintainable, scalable Xperience applications.

### Vertical Slice Architecture

Features are organized by vertical slices in `src/KitchenCommandCenter.Web/Features/`:

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

# Kitchen Command Center

This template should help get you started developing with Vue 3 in Vite.

## Recommended IDE Setup

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Type Support for `.vue` Imports in TS

TypeScript cannot handle type information for `.vue` imports by default, so we replace the `tsc` CLI with `vue-tsc` for type checking. In editors, we need [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) to make the TypeScript language service aware of `.vue` types.

## Customize configuration

See [Vite Configuration Reference](https://vite.dev/config/).

## Project Setup

```sh
yarn
```

### Compile and Hot-Reload for Development

```sh
yarn dev
```

### Type-Check, Compile and Minify for Production

```sh
yarn build
```

### Lint with [ESLint](https://eslint.org/)

```sh
yarn lint
```
