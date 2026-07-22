# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all .csproj files first to optimize Docker layer caching during 'dotnet restore'
COPY ["Project_Sicario_Rebirth.csproj", "./"]
COPY ["src/Sicario_Rebirth.Core/Sicario_Rebirth.Core.csproj", "src/Sicario_Rebirth.Core/"]
COPY ["lib/NetPak/NetPak/NetPak.csproj", "lib/NetPak/NetPak/"]

# Restore dependencies across the entire project tree
RUN dotnet restore "./Project_Sicario_Rebirth.csproj"

# Copy the rest of the source tree (includes core source files and lib code)
COPY . .

WORKDIR "/src/."
RUN dotnet build "./Project_Sicario_Rebirth.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Project_Sicario_Rebirth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Project_Sicario_Rebirth.dll"]