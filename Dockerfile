# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_VERSION=10.0.100

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS dev
WORKDIR /src
COPY global.json ./
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/Bud.Server/Bud.Server.csproj
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "run", "--project", "src/Bud.Server/Bud.Server.csproj", "--urls", "http://0.0.0.0:8080"]
