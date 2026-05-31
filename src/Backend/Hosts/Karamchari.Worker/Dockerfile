FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy directory build props/packages
COPY Directory.Build.props Directory.Packages.props global.json ./

# Copy everything else and build
COPY src/Backend/ src/Backend/

# Restore dependencies
WORKDIR /src/src/Backend/Hosts/Karamchari.Worker
RUN dotnet restore Karamchari.Worker.csproj

# Build and publish
RUN dotnet publish Karamchari.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Install ICU libraries for globalization support (required by Microsoft.Data.SqlClient on Alpine)
RUN apk add --no-cache icu-libs icu-data-full

# Add a non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

# Standardize environment configurations
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Karamchari.Worker.dll"]
