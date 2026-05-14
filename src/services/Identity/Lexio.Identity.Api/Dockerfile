# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level config
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY nuget.config ./

# Restore dependencies
COPY src/ src/
COPY ["Lexio.Identity.Api/Lexio.Identity.Api.csproj", "Lexio.Identity.Api/"]
COPY ["Lexio.Identity.Application/Lexio.Identity.Application.csproj", "Lexio.Identity.Application/"]
COPY ["Lexio.Identity.Domain/Lexio.Identity.Domain.csproj", "Lexio.Identity.Domain/"]
COPY ["Lexio.Identity.Infrastructure/Lexio.Identity.Infrastructure.csproj", "Lexio.Identity.Infrastructure/"]
RUN dotnet restore Lexio.Identity.Api/Lexio.Identity.Api.csproj

# Copy source and publish
COPY . .
RUN dotnet publish Lexio.Identity.Api/Lexio.Identity.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Never run as root in containers
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lexio.Identity.Api.dll"]
