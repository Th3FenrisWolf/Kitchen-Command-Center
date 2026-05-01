# =============================================================================
# Stage 1: Build the .NET application
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src

# Copy solution and project files first for better layer caching
COPY *.sln .
COPY nuget.config .
COPY Directory.Packages.props .
COPY src/KCC.Web/KCC.Web.csproj src/KCC.Web/
COPY src/KCC.Admin/KCC.Admin.csproj src/KCC.Admin/
COPY src/KCC.Core/KCC.Core.csproj src/KCC.Core/

# Restore dependencies (cached unless project files change)
RUN dotnet restore

# Copy the rest of the source code
COPY src/ src/

# =============================================================================
# Stage 2: Build client assets with Node.js
# =============================================================================
FROM node:22-alpine AS node-build
WORKDIR /app

# Copy package files first for better layer caching
COPY src/KCC.Web/package.json src/KCC.Web/yarn.lock* ./

# Install dependencies
RUN yarn install --frozen-lockfile

# Copy source files needed for the build
COPY src/KCC.Web/Features/ ./Features/
COPY src/KCC.Web/vite.config.ts src/KCC.Web/tsconfig*.json ./

# Build client and SSR bundles
RUN yarn build:all

# =============================================================================
# Stage 3: Publish .NET application
# =============================================================================
FROM dotnet-build AS dotnet-publish
WORKDIR /src/src/KCC.Web

# Copy built client assets from node-build stage
COPY --from=node-build /app/wwwroot/assets ./wwwroot/assets/
COPY --from=node-build /app/wwwroot/.vite ./wwwroot/.vite/

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# =============================================================================
# Stage 4: Final runtime image
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published application
COPY --from=dotnet-publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "KCC.Web.dll"]
