# syntax=docker/dockerfile:1

# The client and SSR bundles come out of one `pnpm build:all` in the frontend stage, and both site images copy from
# it: scope IDs hash each component's path and content, so bundles built apart would strip every scoped style.

# CI replaces this empty stage with its pnpm store, which already holds the Font Awesome package, so the install never
# meets that registry's bandwidth quota. Elsewhere the install fetches the package.
FROM scratch AS pnpm-store

# The tag matches packageManager in package.json. On any other version pnpm downloads the declared one on every run.
FROM --platform=$BUILDPLATFORM ghcr.io/pnpm/pnpm:12.17.0 AS pnpm

FROM --platform=$BUILDPLATFORM node:26.13.0-bookworm-slim AS frontend-deps
COPY --from=pnpm /opt/pnpm /opt/pnpm
RUN ln -s /opt/pnpm/pnpm /usr/local/bin/pnpm
WORKDIR /repo
COPY .npmrc package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY packages/ packages/
COPY src/KCC.Web/package.json src/KCC.Web/
COPY src/KCC.Admin/Client/package.json src/KCC.Admin/Client/
COPY src/KCC.Contributions/Client/package.json src/KCC.Contributions/Client/
# pnpm reads no token from a project's .npmrc, so the secret reaches it as the setting's own environment variable.
RUN --mount=type=secret,id=fontawesome,env=FONTAWESOME_NPM_AUTH_TOKEN \
    --mount=type=cache,target=/var/cache/pnpm \
    --mount=type=bind,from=pnpm-store,target=/pnpm-store \
    cp -R /pnpm-store/. /var/cache/pnpm/ \
    && env "pnpm_config_//npm.fontawesome.com/:_authToken=$FONTAWESOME_NPM_AUTH_TOKEN" \
        pnpm install --frozen-lockfile --store-dir /var/cache/pnpm

FROM frontend-deps AS frontend
COPY src/ src/
RUN pnpm build:all \
    && for wwwroot in src/*/wwwroot; do mkdir -p "/out/$wwwroot" && cp -R "$wwwroot/." "/out/$wwwroot/" || exit 1; done

# The SSR service runs on its runtime dependencies alone, none of which comes from Font Awesome's registry.
FROM frontend-deps AS ssr-deps
RUN --mount=type=cache,target=/var/cache/pnpm \
    pnpm --filter kitchen-command-center --prod deploy --ignore-scripts --store-dir /var/cache/pnpm /deps

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build
ARG TARGETARCH
WORKDIR /repo
COPY .editorconfig global.json nuget.config Directory.Build.props Directory.Build.targets Directory.Packages.props KCCStandardRules.ruleset ./
COPY src/KCC.Web/KCC.Web.csproj src/KCC.Web/
COPY src/KCC.Contributions/KCC.Contributions.csproj src/KCC.Contributions/
COPY src/KCC.Admin/KCC.Admin.csproj src/KCC.Admin/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/KCC.Web/KCC.Web.csproj -a $TARGETARCH
COPY src/ src/
COPY --from=frontend /out/ ./
# The Actions cache can supply the restore layer without the packages it fetched, which live only in the cache mount,
# so the publish restores whatever is missing.
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/KCC.Web/KCC.Web.csproj -c Release -a $TARGETARCH -o /app \
    && rm -rf /app/wwwroot/ssr

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS app
# The health check in deploy/compose.yaml needs an HTTP client, which the runtime image lacks.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
# Named volumes mount over umbraco/Data and wwwroot/media and take their owner, so the app can write its database,
# keys, logs and media. Umbraco creates Views/Partials at every start unless it exists, which a read-only root refuses.
RUN mkdir -p umbraco/Data wwwroot/media Views/Partials && chown $APP_UID:$APP_UID umbraco/Data wwwroot/media
USER $APP_UID
ENTRYPOINT ["dotnet", "KCC.Web.dll"]

FROM node:26.13.0-bookworm-slim AS ssr
ENV NODE_ENV=production SSR_PORT=3001
WORKDIR /srv/ssr
COPY --from=ssr-deps /deps/node_modules/ node_modules/
COPY --from=frontend /repo/src/KCC.Web/package.json ./
COPY --from=frontend /repo/src/KCC.Web/Features/Ssr/Server.js /repo/src/KCC.Web/Features/Ssr/CollectCss.js Features/Ssr/
COPY --from=frontend /repo/src/KCC.Web/wwwroot/ssr/ wwwroot/ssr/
USER node
CMD ["node", "Features/Ssr/Server.js"]
