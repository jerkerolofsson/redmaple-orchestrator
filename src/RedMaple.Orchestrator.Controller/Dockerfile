#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RedMaple.Orchestrator.Controller/RedMaple.Orchestrator.Controller.csproj", "RedMaple.Orchestrator.Controller/"]
COPY ["RedMaple.Orchestrator.Containers/RedMaple.Orchestrator.Containers.csproj", "RedMaple.Orchestrator.Containers/"]
COPY ["RedMaple.Orchestrator.Contracts/RedMaple.Orchestrator.Contracts.csproj", "RedMaple.Orchestrator.Contracts/"]
COPY ["RedMaple.Orchestrator.Controller.Domain/RedMaple.Orchestrator.Controller.Domain.csproj", "RedMaple.Orchestrator.Controller.Domain/"]
COPY ["RedMaple.Orchestrator.Sdk/RedMaple.Orchestrator.Sdk.csproj", "RedMaple.Orchestrator.Sdk/"]
COPY ["RedMaple.Orchestrator.Security/RedMaple.Orchestrator.Security.csproj", "RedMaple.Orchestrator.Security/"]
COPY ["RedMaple.Orchestrator.Controller.Infrastructure/RedMaple.Orchestrator.Controller.Infrastructure.csproj", "RedMaple.Orchestrator.Controller.Infrastructure/"]
COPY ["RedMaple.Orchestrator.Dns/RedMaple.Orchestrator.Dns.csproj", "RedMaple.Orchestrator.Dns/"]
COPY ["RedMaple.Orchestrator.Ingress/RedMaple.Orchestrator.Ingress.csproj", "RedMaple.Orchestrator.Ingress/"]
COPY ["RedMaple.Orchestrator.ServiceDefaults/RedMaple.Orchestrator.ServiceDefaults.csproj", "RedMaple.Orchestrator.ServiceDefaults/"]
RUN dotnet restore "./RedMaple.Orchestrator.Controller/RedMaple.Orchestrator.Controller.csproj"
COPY . .
WORKDIR "/src/RedMaple.Orchestrator.Controller"
RUN dotnet build "./RedMaple.Orchestrator.Controller.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RedMaple.Orchestrator.Controller.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /data/redmaple/controller
ENTRYPOINT ["dotnet", "RedMaple.Orchestrator.Controller.dll"]