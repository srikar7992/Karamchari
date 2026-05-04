FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy Directory.Build.props and Directory.Packages.props
COPY Directory.Build.props Directory.Packages.props ./

# Copy csproj files and restore
COPY src/Backend/Karamchari.Api/Karamchari.Api.csproj src/Backend/Karamchari.Api/
COPY src/Backend/Karamchari.Core/Karamchari.Core.csproj src/Backend/Karamchari.Core/
COPY src/Backend/Karamchari.HR/Karamchari.HR.csproj src/Backend/Karamchari.HR/
COPY src/Backend/Karamchari.Payroll/Karamchari.Payroll.csproj src/Backend/Karamchari.Payroll/
RUN dotnet restore src/Backend/Karamchari.Api/Karamchari.Api.csproj

# Copy everything else and build
COPY src/Backend/ src/Backend/
WORKDIR /app/src/Backend/Karamchari.Api
RUN dotnet publish Karamchari.Api.csproj -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Karamchari.Api.dll"]
