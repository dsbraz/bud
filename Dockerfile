# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_VERSION=10.0.102

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS source
WORKDIR /src
COPY global.json ./
COPY Directory.Build.props ./
COPY src/ ./src/

FROM source AS dev-web
RUN dotnet restore src/Bud.Server/Bud.Server.csproj
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "run", "--project", "src/Bud.Server/Bud.Server.csproj", "--urls", "http://0.0.0.0:8080"]

FROM source AS dev-mcp
RUN dotnet restore src/Bud.Mcp/Bud.Mcp.csproj
