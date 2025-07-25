$version="1.0.0"

$package="RedMaple.Orchestrator.Contracts"
cd src/${package}
dotnet pack -p:PackageVersion=$version
nuget push bin/Release/${package}.${version}.nupkg -Source https://api.nuget.org/v3/index.json
cd ../..

$package="RedMaple.Orchestrator.Containers"
cd src/${package}
dotnet pack -p:PackageVersion=$version
nuget push bin/Release/${package}.${version}.nupkg -Source https://api.nuget.org/v3/index.json
cd ../..

$package="RedMaple.Orchestrator.DockerCompose"
cd src/${package}
dotnet pack -p:PackageVersion=$version
nuget push ./bin/Release/${package}.${version}.nupkg -Source https://api.nuget.org/v3/index.json
cd ../..
