using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var controller = builder.AddProject<Projects.RedMaple_Orchestrator_Controller>("controller")
    .WithEnvironment("REDMAPLE_ENCR_KEY", "763eb0161deca428847060f94e9879413d1eb1f23f6ce23d6558f4c198f2ab88/65ab53168877b909abc2cde3448413b4")
    .WithEnvironment("REDMAPLE_DEFAULT_DOMAIN", "local")
    .WithEnvironment("REDMAPLE_SECRET_PASSWORD", "password");

builder.AddProject<Projects.RedMaple_Orchestrator_Node>("node")
    .WithEnvironment("NODE_IP", "172.18.30.118")
    .WithEnvironment("REDMAPLE_ENCR_KEY", "763eb0161deca428847060f94e9879413d1eb1f23f6ce23d6558f4c198f2ab88/65ab53168877b909abc2cde3448413b4")
    .WithReference(controller);

builder.Build().Run();
