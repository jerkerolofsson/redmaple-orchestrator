using RedMaple.Orchestrator.Node.Enrollment;
using RedMaple.Orchestrator.Node.LocalDeployments;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddContainerServices();

builder.Services.AddHostedService<EnrollAtStartup>();
builder.Services.AddHostedService<StartDnsAtStartup>();
builder.Services.AddHostedService<StartIngressAtStartup>();
builder.Services.AddMediatR(options => 
{
    options.RegisterServicesFromAssembly(typeof(EnrollAtStartup).Assembly);
});

builder.Services.AddTransient<RedMaple.Orchestrator.DockerCompose.IDockerCompose, RedMaple.Orchestrator.DockerCompose.DockerCompose>();
builder.Services.AddSingleton<ILocalDeploymentService, LocalDeploymentService>();
builder.Services.AddSingleton<INodeSettingsProvider, NodeSettingsProvider>();
builder.Services.AddDns().AddIngress();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
