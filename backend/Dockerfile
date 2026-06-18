# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching). Both projects share the same folder.
COPY llmmo.csproj ./
COPY WorldSetup/WorldSetup.csproj WorldSetup/
RUN dotnet restore llmmo.csproj \
    && dotnet restore WorldSetup/WorldSetup.csproj

# Copy the rest of the source and publish both the API and the seeder.
COPY . .
RUN dotnet publish llmmo.csproj -c Release -o /app/api --no-restore \
    && dotnet publish WorldSetup/WorldSetup.csproj -c Release -o /app/seed --no-restore

# ---- API runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=build /app/api ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "llmmo.dll"]

# ---- Seeder runtime (one-shot world reset + demo data) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS seed
WORKDIR /app
COPY --from=build /app/seed ./
ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "WorldSetup.dll"]
