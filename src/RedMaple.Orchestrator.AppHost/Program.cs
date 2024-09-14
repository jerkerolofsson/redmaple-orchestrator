using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var controller = builder.AddProject<Projects.RedMaple_Orchestrator_Controller>("controller")
        .WithEnvironment("REDMAPLE_DEFAULT_DOMAIN", "local")
        .WithEnvironment("OIDC_AUTHORITY", "https://identity.ec.lan/node/oidc/v2.0/ecordia/");

builder.AddProject<Projects.RedMaple_Orchestrator_Node>("node")
    .WithEnvironment("NODE_IP", "172.18.30.118")
    .WithReference(controller);

builder.Build().Run();
