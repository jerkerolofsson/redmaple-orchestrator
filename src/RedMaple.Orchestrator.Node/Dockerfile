#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RedMaple.Orchestrator.Node/RedMaple.Orchestrator.Node.csproj", "RedMaple.Orchestrator.Node/"]
COPY ["RedMaple.Orchestrator.Containers/RedMaple.Orchestrator.Containers.csproj", "RedMaple.Orchestrator.Containers/"]
COPY ["RedMaple.Orchestrator.Contracts/RedMaple.Orchestrator.Contracts.csproj", "RedMaple.Orchestrator.Contracts/"]
COPY ["RedMaple.Orchestrator.ServiceDefaults/RedMaple.Orchestrator.ServiceDefaults.csproj", "RedMaple.Orchestrator.ServiceDefaults/"]
RUN dotnet restore "./RedMaple.Orchestrator.Node/RedMaple.Orchestrator.Node.csproj"
COPY . .
WORKDIR "/src/RedMaple.Orchestrator.Node"
RUN dotnet build "./RedMaple.Orchestrator.Node.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RedMaple.Orchestrator.Node.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedMaple.Orchestrator.Node.dll"]