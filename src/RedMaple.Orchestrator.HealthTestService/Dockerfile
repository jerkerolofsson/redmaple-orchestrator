#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 14911
EXPOSE 14912

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RedMaple.Orchestrator.HealthTestService/RedMaple.Orchestrator.HealthTestService.csproj", "RedMaple.Orchestrator.HealthTestService/"]
RUN dotnet restore "./RedMaple.Orchestrator.HealthTestService/RedMaple.Orchestrator.HealthTestService.csproj"
COPY . .
WORKDIR "/src/RedMaple.Orchestrator.HealthTestService"
RUN dotnet build "./RedMaple.Orchestrator.HealthTestService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RedMaple.Orchestrator.HealthTestService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedMaple.Orchestrator.HealthTestService.dll"]