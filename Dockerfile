# syntax=docker/dockerfile:1
# ─────────────────────────────────────────────────────────────────────────────
# TaxiBlitz Ohrid — ASP.NET Core 10 MVC
# Multi-stage, multi-arch (linux/amd64 + linux/arm64) image.
#   build   : restores and publishes on the *build machine's* native arch and
#             cross-targets TARGETARCH, so no slow QEMU emulation of the SDK
#   runtime : slim ASP.NET runtime, non-root user, only upload dirs writable
# ─────────────────────────────────────────────────────────────────────────────

# ---------- build ----------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

# 1) Project files only -> restore is cached until a .csproj changes
COPY src/TaxiBlitz.Domain/TaxiBlitz.Domain.csproj                 src/TaxiBlitz.Domain/
COPY src/TaxiBlitz.Shared/TaxiBlitz.Shared.csproj                 src/TaxiBlitz.Shared/
COPY src/TaxiBlitz.Application/TaxiBlitz.Application.csproj       src/TaxiBlitz.Application/
COPY src/TaxiBlitz.Persistence/TaxiBlitz.Persistence.csproj       src/TaxiBlitz.Persistence/
COPY src/TaxiBlitz.Infrastructure/TaxiBlitz.Infrastructure.csproj src/TaxiBlitz.Infrastructure/
COPY src/TaxiBlitz.Web/TaxiBlitz.Web.csproj                       src/TaxiBlitz.Web/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/TaxiBlitz.Web/TaxiBlitz.Web.csproj -a $TARGETARCH

# 2) Source code -> publish
COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/TaxiBlitz.Web/TaxiBlitz.Web.csproj \
      -c Release -a $TARGETARCH --no-restore \
      -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ARG VERSION=dev
ARG GIT_SHA=unknown
LABEL org.opencontainers.image.title="taxiblitz-web" \
      org.opencontainers.image.description="TaxiBlitz Ohrid — taxi tour booking platform (ASP.NET Core 10 MVC)" \
      org.opencontainers.image.source="https://github.com/edirizvani/taxiblitz-kiii" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.revision="${GIT_SHA}"

# curl is only used by the HEALTHCHECK below
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# App files stay root-owned (read-only for the app user); only the folders the
# app writes to at runtime (uploads, fallback key ring) belong to the app user.
RUN mkdir -p wwwroot/gallery wwwroot/profile-pics DataProtection-Keys \
 && chown -R $APP_UID:$APP_UID wwwroot/gallery wwwroot/profile-pics DataProtection-Keys

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    APP_VERSION=${VERSION}
EXPOSE 8080

USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "TaxiBlitz.Web.dll"]
