using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var controller = builder.AddProject<Projects.RedMaple_Orchestrator_Controller>("controller")
        .WithEnvironment("NODE_IP", "192.168.0.241");

builder.AddProject<Projects.RedMaple_Orchestrator_Node>("node")
    .WithEnvironment("NODE_IP", "192.168.0.241")
    .WithReference(controller);

builder.AddProject<Projects.RedMaple_Orchestrator_Controller_Api>("redmaple-orchestrator-controller-api");

builder.Build().Run();
