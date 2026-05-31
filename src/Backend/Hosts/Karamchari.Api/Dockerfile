FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy directory build props/packages
COPY Directory.Build.props Directory.Packages.props global.json ./

# Copy the solution file (if it exists in the root, assuming src/Backend/Karamchari.sln)
COPY src/Backend/Karamchari.sln src/Backend/

# Copy all .csproj files preserving directory structure
COPY src/Backend/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/Backend/${file%.*}/ && mv $file src/Backend/${file%.*}/; done

# Alternatively, just copy the entire Backend folder (less efficient but guarantees all projects are there for the solution restore)
COPY src/Backend/ src/Backend/

# Restore dependencies
WORKDIR /src/src/Backend/Karamchari.Api
RUN dotnet restore Karamchari.Api.csproj

# Build and publish
RUN dotnet publish Karamchari.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install ICU libraries for globalization support (required by Microsoft.Data.SqlClient on Alpine)
RUN apk add --no-cache icu-libs icu-data-full

# Add a non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

# Health check probe
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

COPY --from=build /app/publish .

# Standardize environment configurations
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080
ENTRYPOINT ["dotnet", "Karamchari.Api.dll"]
