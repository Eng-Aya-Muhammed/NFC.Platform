# Stage 1: Runtime Base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: SDK Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project definition files to restore dependencies in a cached layer
COPY ["NFC.Platform.API/NFC.Platform.API.csproj", "NFC.Platform.API/"]
COPY ["NFC.Platform.Application/NFC.Platform.Application.csproj", "NFC.Platform.Application/"]
COPY ["NFC.Platform.Domain/NFC.Platform.Domain.csproj", "NFC.Platform.Domain/"]
COPY ["NFC.Platform.Infrastructure/NFC.Platform.Infrastructure.csproj", "NFC.Platform.Infrastructure/"]
COPY ["NFC.Platform.BuildingBlocks/NFC.Platform.BuildingBlocks.csproj", "NFC.Platform.BuildingBlocks/"]

RUN dotnet restore "NFC.Platform.API/NFC.Platform.API.csproj"

# Copy the rest of the codebase
COPY . .

# Build and Publish
WORKDIR "/src/NFC.Platform.API"
RUN dotnet publish "NFC.Platform.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NFC.Platform.API.dll"]
