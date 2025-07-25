#$DOCKER_REGISTRY = "192.168.0.241:5000"
#$DOCKER_REGISTRY = "docker-repo.dev.ec.lan:5005"
$DOCKER_REGISTRY = "docker-new.ec.lan:17326"

cd src

echo "Building ${DOCKER_REGISTRY}/redmaple-node.."

nuget restore RedMaple.Orchestrator.sln 
  
cd RedMaple.Orchestrator.Node 
dotnet publish --os linux --arch x64 -p ContainerRepository=$DOCKER_REGISTRY/redmaple-node /t:PublishContainer
docker push ${DOCKER_REGISTRY}/redmaple-node
cd ..

cd RedMaple.Orchestrator.Controller
dotnet publish --os linux --arch x64 -p ContainerRepository=$DOCKER_REGISTRY/redmaple-controller /t:PublishContainer
docker push ${DOCKER_REGISTRY}/redmaple-controller
cd ..

cd RedMaple.Orchestrator.Cli
dotnet publish --os linux --arch x64 
dotnet publish --os win --arch x64 
cd ..

cd ..
