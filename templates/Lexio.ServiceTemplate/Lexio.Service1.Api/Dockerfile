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
COPY ["Lexio.Service1.Api/Lexio.Service1.Api.csproj", "Lexio.Service1.Api/"]
COPY ["Lexio.Service1.Application/Lexio.Service1.Application.csproj", "Lexio.Service1.Application/"]
COPY ["Lexio.Service1.Domain/Lexio.Service1.Domain.csproj", "Lexio.Service1.Domain/"]
COPY ["Lexio.Service1.Infrastructure/Lexio.Service1.Infrastructure.csproj", "Lexio.Service1.Infrastructure/"]
RUN dotnet restore Lexio.Service1.Api/Lexio.Service1.Api.csproj

# Copy source and publish
COPY . .
RUN dotnet publish Lexio.Service1.Api/Lexio.Service1.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Never run as root in containers
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lexio.Service1.Api.dll"]
