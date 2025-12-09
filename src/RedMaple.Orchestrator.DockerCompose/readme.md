
This library handles parsing of docker-compose files.

# Parsing example

```csharp
using RedMaple.Orchestrator.DockerCompose;

DockerComposePlan plan = DockerComposeParser.Parse("docker-compose.yaml");
```


# Example to bring up a container

```csharp
using RedMaple.Orchestrator.DockerCompose;
using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

class ConsoleLogger : IProgress<string> 
{
    public void Report(string value)
    {
        Console.WriteLine(value);
    }
}

var services = new ServiceCollection();
services.AddDockerCompose();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Parse the plan
DockerComposePlan plan = DockerComposeParser.Parse("docker-compose.yaml");

// Write progress to console
var progressLogger = new ConsoleLogger();

// Bring up container
var docker = serviceProvider.GetRequiredService<IDockerCompose>();
await docker.UpAsync(progressLogger, plan, environmentFile: null, cancellationToken: default);

// Take down container
await docker.DownAsync(progressLogger, plan, environmentFile: null, cancellationToken: default);
```