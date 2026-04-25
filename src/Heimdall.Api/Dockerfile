# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files (relative to build context: src/Heimdall.Api/)
COPY ["Heimdall.Api.csproj", "Heimdall.Api/"]
COPY ["../Heimdall.Application/Heimdall.Application.csproj", "Heimdall.Application/"]
COPY ["../Heimdall.Domain/Heimdall.Domain.csproj", "Heimdall.Domain/"]
COPY ["../Heimdall.Infrastructure/Heimdall.Infrastructure.csproj", "Heimdall.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Heimdall.Api/Heimdall.Api.csproj"

# Copy all source code from parent directory
COPY . Heimdall.Api/
COPY ../Heimdall.Application/ Heimdall.Application/
COPY ../Heimdall.Domain/ Heimdall.Domain/
COPY ../Heimdall.Infrastructure/ Heimdall.Infrastructure/

# Build and publish
WORKDIR "/src/Heimdall.Api"
RUN dotnet publish "Heimdall.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Create directory for SQLite database (if needed)
RUN mkdir -p /app/data

# Expose port
EXPOSE 5000

# Environment variables (defaults)
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Heimdall.Api.dll"]
