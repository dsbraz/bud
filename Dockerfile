# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_VERSION=10.0.102

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS source
WORKDIR /src
COPY global.json ./
COPY Directory.Build.props ./
COPY src/ ./src/

FROM source AS dev-web
RUN dotnet restore src/Server/Bud.Api/Bud.Api.csproj
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1
ENTRYPOINT ["sh", "-c", "dotnet restore src/Server/Bud.Api/Bud.Api.csproj && dotnet watch --non-interactive --project src/Server/Bud.Api/Bud.Api.csproj run --urls http://0.0.0.0:8080 -p:WasmEnableHotReload=true"]

FROM source AS dev-mcp
RUN dotnet restore src/Bud.Mcp/Bud.Mcp.csproj

FROM source AS dev-mcp-web
RUN dotnet restore src/Bud.Mcp/Bud.Mcp.csproj
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1
ENTRYPOINT ["sh", "-c", "dotnet restore src/Bud.Mcp/Bud.Mcp.csproj && dotnet watch --non-interactive --project src/Bud.Mcp/Bud.Mcp.csproj run --urls http://0.0.0.0:8080"]
