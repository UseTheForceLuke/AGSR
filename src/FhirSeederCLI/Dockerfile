# Build stage
FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src

# Install curl for health checks
RUN apt-get update && apt-get install -y curl

# Copy project file first for better caching
COPY ["src/FhirSeederCLI/FhirSeederCLI.csproj", "."]
RUN dotnet restore "FhirSeederCLI.csproj"

# Copy everything else
COPY "src/FhirSeederCLI/" .
WORKDIR "/src"

# Build with explicit settings
RUN dotnet build "FhirSeederCLI.csproj" -c Release -o /app/build \
    /p:GenerateProgramFile=false

# Publish stage
FROM build AS publish
RUN dotnet publish "FhirSeederCLI.csproj" -c Release -o /app/publish \
    /p:GenerateProgramFile=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:3.1 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Use shell form for better debugging
ENTRYPOINT dotnet FhirSeederCLI.dll