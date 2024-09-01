using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var controller = builder.AddProject<Projects.RedMaple_Orchestrator_Controller>("controller")
        .WithEnvironment("INGRESS_IP", "192.168.0.241");
builder.AddProject<Projects.RedMaple_Orchestrator_Node>("node")
    .WithReference(controller);

builder.Build().Run();
