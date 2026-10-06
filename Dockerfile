# syntax=docker/dockerfile:1

# The client and SSR bundles come out of one `yarn build:all` in the frontend stage, and both site images copy from
# it: scope IDs hash each component's path and content, so bundles built apart would strip every scoped style.

FROM --platform=$BUILDPLATFORM node:24.21.0-bookworm-slim AS frontend
WORKDIR /repo
COPY package.json yarn.lock ./
COPY packages/ packages/
COPY src/KCC.Web/package.json src/KCC.Web/
COPY src/KCC.Admin/Client/package.json src/KCC.Admin/Client/
COPY src/KCC.Contributions/Client/package.json src/KCC.Contributions/Client/
# Yarn reads .npmrc on every command and fails while its token variable is unset, so only the install sees it.
RUN --mount=type=bind,source=.npmrc,target=.npmrc \
    --mount=type=secret,id=fontawesome,env=FONTAWESOME_NPM_AUTH_TOKEN \
    --mount=type=cache,target=/usr/local/share/.cache/yarn \
    yarn install --frozen-lockfile --network-timeout 600000
COPY src/ src/
RUN yarn build:all \
    && for wwwroot in src/*/wwwroot; do mkdir -p "/out/$wwwroot" && cp -R "$wwwroot/." "/out/$wwwroot/" || exit 1; done

FROM --platform=$BUILDPLATFORM node:24.21.0-bookworm-slim AS ssr-deps
WORKDIR /deps
COPY src/KCC.Web/package.json yarn.lock ./
# Yarn fetches devDependencies even for a production install, and Font Awesome's registry wants a token, so the SSR
# service's install reads a manifest with its runtime dependencies only. Its cache mount has its own id because Yarn 1
# does not lock its cache and this install runs beside the frontend stage's.
RUN --mount=type=cache,id=yarn-ssr,target=/usr/local/share/.cache/yarn \
    node -e "const fs = require('fs'); const p = JSON.parse(fs.readFileSync('package.json')); delete p.devDependencies; fs.writeFileSync('package.json', JSON.stringify(p))" \
    && yarn install --production --frozen-lockfile --ignore-scripts --network-timeout 600000

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS app
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

FROM node:24.21.0-bookworm-slim AS ssr
ENV NODE_ENV=production SSR_PORT=3001
WORKDIR /srv/ssr
COPY --from=ssr-deps /deps/node_modules/ node_modules/
COPY --from=frontend /repo/src/KCC.Web/package.json ./
COPY --from=frontend /repo/src/KCC.Web/Features/Ssr/Server.js /repo/src/KCC.Web/Features/Ssr/CollectCss.js Features/Ssr/
COPY --from=frontend /repo/src/KCC.Web/wwwroot/ssr/ wwwroot/ssr/
USER node
CMD ["node", "Features/Ssr/Server.js"]
