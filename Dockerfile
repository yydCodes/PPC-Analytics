# Payroll Intelligence Assistant - Docker Configuration
# Multi-stage build for optimized image size

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for better caching)
COPY PayrollIntelligence.sln .
COPY PayrollIntelligence.Core/PayrollIntelligence.Core.csproj PayrollIntelligence.Core/
COPY PayrollIntelligence.Web/PayrollIntelligence.Web.csproj PayrollIntelligence.Web/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build the web application
WORKDIR /src/PayrollIntelligence.Web
RUN dotnet build -c Release -o /app/build

# ============================================
# Stage 2: Publish
# ============================================
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/Home/Index || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PayrollIntelligence.Web.dll"]
