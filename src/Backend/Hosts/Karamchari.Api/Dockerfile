FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy directory build props/packages
COPY Directory.Build.props Directory.Packages.props global.json ./

# Copy the solution file
COPY src/Backend/Karamchari.sln src/Backend/

# Copy the entire Backend folder
COPY src/Backend/ src/Backend/

# Restore dependencies
WORKDIR /src/src/Backend/Hosts/Karamchari.Api
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
