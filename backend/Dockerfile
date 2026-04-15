# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y libkrb5-3 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and all projects
COPY kodeklubb_prosjekt.sln .
COPY src/Api ./src/Api
COPY src/Core ./src/Core
COPY src/Persistence ./src/Persistence
COPY src/Worker ./src/Worker

# Restore dependencies for the whole solution
RUN dotnet restore kodeklubb_prosjekt.sln

# Build the Api project (pulls in all dependencies)
RUN dotnet build src/Api/Api.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/Api/Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
