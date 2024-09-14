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
cd ..
