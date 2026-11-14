# Code Structure & Implementation Details

## File Organization

```
redmaple-orchestrator/
├── src/
│   ├── RedMaple.Orchestration.Containers.IntegrationTests/
│   │   ├── CreateTests.cs                    ← NEW: Comprehensive test suite
│   │   ├── ImageTests.cs                     (existing - unchanged)
│   │   ├── LocalContainerBackendFixture.cs   (existing - unchanged)
│   │   └── TempDirectory.cs                  ← MODIFIED: Added Path property
│   │
│   └── RedMaple.Orchestrator.Containers/
│       └── Wslc/
│           └── WslcClient.cs                 ← MODIFIED: Fixed CreateContainerAsync
│
└── session-state/files/
	├── COMPLETION_REPORT.md                  ← Full completion summary
	├── TEST_IMPLEMENTATION_SUMMARY.md        ← Test overview
	├── IMPLEMENTATION_DETAILS.md             ← Technical deep dive
	├── QUICK_REFERENCE.md                    ← Quick start guide
	└── EXAMPLE_TEST_EXECUTION.md             ← Execution examples
```

---

## Class Hierarchy & Dependencies

```
Test Execution
	  │
	  ├─→ CreateTests (xUnit.net)
	  │        │
	  │        ├─→ GetAlpineImageAsync()
	  │        │        └─→ ILocalContainersClient.HasImageAsync()
	  │        │        └─→ ILocalContainersClient.PullImageAsync()
	  │        │
	  │        ├─→ GetRegistryImageAsync()
	  │        │        └─→ ILocalContainersClient.HasImageAsync()
	  │        │        └─→ ILocalContainersClient.PullImageAsync()
	  │        │
	  │        └─→ [5 Test Methods]
	  │                └─→ LocalContainerBackendFixture
	  │                     │
	  │                     └─→ ILocalContainersClient (interface)
	  │                         │
	  │                         ├─→ LocalContainersClient (Docker)
	  │                         │    └─→ DockerClientConfiguration
	  │                         │         └─→ Docker.DotNet
	  │                         │
	  │                         ├─→ PodmanContainerClient (Podman)
	  │                         │    └─→ DockerClientConfiguration
	  │                         │         └─→ Docker.DotNet (via Podman API)
	  │                         │
	  │                         └─→ WslcClient (WSL Containers)
	  │                              └─→ ProcessStartInfo
	  │                                   └─→ wslc CLI
	  │
	  └─→ TempDirectory (IDisposable)
		   └─→ Path property (for volume binds)
```

---

## CreateTests.cs Class Structure

```csharp
public class CreateTests
{
	// ─────────────────────────────────────
	// Helper Methods
	// ─────────────────────────────────────

	private async Task<string> GetAlpineImageAsync(ILocalContainersClient client)
	{
		// Purpose: Ensure alpine:latest available
		// Returns: "alpine:latest"
		// Auto-pulls if missing
	}

	private async Task<string> GetRegistryImageAsync(ILocalContainersClient client)
	{
		// Purpose: Ensure registry:2 available
		// Returns: "registry:2"
		// Auto-pulls if missing
	}

	// ─────────────────────────────────────
	// Test Methods [Theory + InlineData]
	// ─────────────────────────────────────

	[Theory]
	[InlineData(ContainerBackend.Docker)]
	[InlineData(ContainerBackend.Podman)]
	[InlineData(ContainerBackend.WslContainers)]
	public async Task CreateContainer_IsSuccessful(ContainerBackend backend)
	{
		// ✅ Test 1: Basic container creation
	}

	[Theory]
	[InlineData(ContainerBackend.Docker)]
	[InlineData(ContainerBackend.Podman)]
	[InlineData(ContainerBackend.WslContainers)]
	public async Task CreateAndDeleteContainer_ContainerIsRemovedFromListing(
		ContainerBackend backend)
	{
		// ✅ Test 2: Container deletion & listing
	}

	[Theory]
	[InlineData(ContainerBackend.Docker)]
	[InlineData(ContainerBackend.Podman)]
	[InlineData(ContainerBackend.WslContainers)]
	public async Task CreateContainer_WithVolumeMount_IsSuccessful(
		ContainerBackend backend)
	{
		// ✅ Test 3: Volume mount binding
	}

	[Theory]
	[InlineData(ContainerBackend.Docker)]
	[InlineData(ContainerBackend.Podman)]
	[InlineData(ContainerBackend.WslContainers)]
	public async Task CreateContainer_WithVolumeMount_ExecAndVerifyFile(
		ContainerBackend backend)
	{
		// ✅ Test 4: Volume roundtrip (exec + file verification)
	}

	[Theory]
	[InlineData(ContainerBackend.Docker)]
	[InlineData(ContainerBackend.Podman)]
	[InlineData(ContainerBackend.WslContainers)]
	public async Task CreateContainer_WithPortMapping_IsSuccessful(
		ContainerBackend backend)
	{
		// ✅ Test 5: Port mapping & HTTP connectivity
	}
}
```

---

## WslcClient.CreateContainerAsync() Enhanced Implementation

```csharp
public async Task<CreateContainerResponse> CreateContainerAsync(
	CreateContainerParameters parameters, 
	CancellationToken cancellationToken = default)
{
	// ─────────────────────────────────────
	// Step 1: Build wslc command
	// ─────────────────────────────────────
	var args = new StringBuilder("run -d");

	// ─────────────────────────────────────
	// Step 2: Add container name
	// ─────────────────────────────────────
	if (!string.IsNullOrEmpty(parameters.Name))
	{
		args.Append($" --name {EscapeArgument(parameters.Name)}");
	}

	// ─────────────────────────────────────
	// Step 3: Add environment variables
	// ─────────────────────────────────────
	if (parameters.Env?.Count > 0)
	{
		foreach (var env in parameters.Env)
		{
			args.Append($" -e {EscapeArgument(env)}");
		}
	}

	// ─────────────────────────────────────
	// Step 4: Add port bindings (ENHANCED)
	// ─────────────────────────────────────
	if (parameters.HostConfig?.PortBindings?.Count > 0)
	{
		// Format: -p hostPort:containerPort
		// Example: -p 5555:5000
		foreach (var portBinding in parameters.HostConfig.PortBindings)
		{
			var containerPort = portBinding.Key;  // "5000/tcp"
			foreach (var binding in portBinding.Value)
			{
				if (!string.IsNullOrEmpty(binding.HostPort))
				{
					args.Append(
						$" -p {EscapeArgument(
							$"{binding.HostPort}:{containerPort}")}"
					);
				}
				else
				{
					args.Append($" -p {EscapeArgument(containerPort)}");
				}
			}
		}
	}

	// ─────────────────────────────────────
	// Step 5: Add exposed ports (fallback)
	// ─────────────────────────────────────
	if (parameters.ExposedPorts?.Count > 0)
	{
		foreach (var port in parameters.ExposedPorts.Keys)
		{
			// Only add if not already in PortBindings
			if (parameters.HostConfig?.PortBindings == null || 
				!parameters.HostConfig.PortBindings.ContainsKey(port))
			{
				args.Append($" -p {EscapeArgument(port)}");
			}
		}
	}

	// ─────────────────────────────────────
	// Step 6: Add volume binds (FIXED)
	// ─────────────────────────────────────
	// NEW: Prioritize Binds from HostConfig
	if (parameters.HostConfig?.Binds?.Count > 0)
	{
		// Format: -v hostPath:containerPath
		// Example: -v C:\temp:/volume
		foreach (var bind in parameters.HostConfig.Binds)
		{
			args.Append($" -v {EscapeArgument(bind)}");
		}
	}
	// NEW: Only use bare Volumes if no Binds
	else if (parameters.Volumes?.Count > 0)
	{
		foreach (var vol in parameters.Volumes.Keys)
		{
			args.Append($" -v {EscapeArgument(vol)}");
		}
	}

	// ─────────────────────────────────────
	// Step 7: Add privileges if needed
	// ─────────────────────────────────────
	if (parameters.HostConfig?.Privileged == true)
	{
		args.Append(" --privileged");
	}

	// ─────────────────────────────────────
	// Step 8: Add image name
	// ─────────────────────────────────────
	args.Append($" {EscapeArgument(parameters.Image)}");

	// ─────────────────────────────────────
	// Step 9: Add command (if any)
	// ─────────────────────────────────────
	if (parameters.Cmd?.Count > 0)
	{
		foreach (var cmd in parameters.Cmd)
		{
			args.Append($" {EscapeArgument(cmd)}");
		}
	}

	// ─────────────────────────────────────
	// Step 10: Execute wslc command
	// ─────────────────────────────────────
	var (exitCode, stdout, stderr) = 
		await ExecuteWslcCommandAsync(args.ToString(), cancellationToken);

	// ─────────────────────────────────────
	// Step 11: Handle result
	// ─────────────────────────────────────
	if (exitCode != 0)
	{
		_logger?.LogError("Failed to create container: {Error}", stderr);
		throw new InvalidOperationException(
			$"Failed to create container: {stderr}");
	}

	// ─────────────────────────────────────
	// Step 12: Return container ID
	// ─────────────────────────────────────
	var containerId = stdout.Trim();
	return new CreateContainerResponse { ID = containerId };
}
```

---

## Test Execution Sequence Diagram

```
Time    Action
────────────────────────────────────────────────────────────
T+0s    [Test Start]
		├─ Create LocalContainerBackendFixture(Docker)
		└─ Setup DI container and get ILocalContainersClient

T+0.1s  [Get Image]
		├─ Call GetAlpineImageAsync()
		├─ Check: HasImageAsync("alpine:latest")
		│  ├─ If false → PullImageAsync("alpine:latest")
		│  └─ Takes ~1-2s on first run
		└─ Return image name

T+1.5s  [Create Container]
		├─ CreateContainerAsync(parameters)
		│  ├─ Generate unique name: test-create-a1b2c3d4
		│  ├─ Set image: alpine:latest
		│  ├─ Set cmd: ["sleep", "1000"]
		│  └─ Return container ID
		└─ Container is created but not running

T+2.0s  [Verify Creation]
		├─ GetContainersAsync(null)
		├─ Search for container by ID
		├─ Verify name matches
		└─ Assert passes ✅

T+2.1s  [Cleanup]
		├─ DeleteContainerAsync(containerId)
		└─ Container removed

T+2.3s  [Test End]
		└─ PASSED ✅
```

---

## Docker.DotNet Parameter Mapping

### CreateContainerParameters → wslc run

| Docker.DotNet | wslc flag | Example |
|---------------|-----------|---------|
| Name | --name | --name my-container |
| Env | -e | -e PATH=/usr/bin |
| Image | (positional) | alpine:latest |
| Cmd | (positional) | sleep 1000 |
| Volumes | -v (path only) | -v /data |
| HostConfig.Binds | -v (host:container) | -v /tmp:/data |
| HostConfig.PortBindings | -p (host:container) | -p 5555:5000 |
| HostConfig.Privileged | --privileged | --privileged |

---

## Error Handling Flow

```
CreateContainerAsync()
	│
	├─→ Build command arguments
	│
	├─→ Execute wslc CLI
	│
	└─→ Check exit code
		 │
		 ├─ exitCode == 0
		 │  ├─ Extract container ID from stdout
		 │  └─ Return CreateContainerResponse { ID }
		 │
		 └─ exitCode != 0
			├─ Log error with stderr
			├─ Throw InvalidOperationException
			└─ Propagate to test
				 │
				 └─ Test catch/finally block executes
					├─ Attempt cleanup
					└─ Graceful failure

Test catch/finally
	│
	├─ StopAsync() [if needed]
	│
	├─ DeleteContainerAsync() [always]
	│
	├─ TempDirectory.Dispose()
	│  └─ try/catch ensures cleanup even on error
	│
	└─ Test fails with original exception + cleanup info
```

---

## Resource Lifecycle

```
Test Lifecycle:
┌─────────────────────────────────────────────────────┐
│ Setup Phase                                         │
├─────────────────────────────────────────────────────┤
│ 1. Create LocalContainerBackendFixture              │
│    - Build ServiceProvider                          │
│    - Register services                              │
│    - Get ILocalContainersClient                     │
│                                                     │
│ 2. Ensure images available                          │
│    - Check HasImageAsync()                          │
│    - PullImageAsync() if needed                     │
│                                                     │
│ 3. Create TempDirectory (if volume test)            │
│    - Create random temp path                        │
│    - Create directory on disk                       │
└─────────────────────────────────────────────────────┘
					  ↓
┌─────────────────────────────────────────────────────┐
│ Execution Phase                                     │
├─────────────────────────────────────────────────────┤
│ 1. CreateContainerAsync()                           │
│    - Generate unique container name                 │
│    - Call backend API (Docker/Podman/wslc)         │
│    - Return container ID                            │
│                                                     │
│ 2. Manipulate container                             │
│    - StartAsync()                                   │
│    - ExecAsync()                                    │
│    - GetContainersAsync()                           │
│                                                     │
│ 3. Assert results                                   │
│    - Verify container state                         │
│    - Verify file writes                             │
│    - Verify HTTP responses                          │
└─────────────────────────────────────────────────────┘
					  ↓
┌─────────────────────────────────────────────────────┐
│ Cleanup Phase (Finally Block)                       │
├─────────────────────────────────────────────────────┤
│ 1. StopAsync(containerId)                           │
│    - Send SIGTERM to container                      │
│    - Wait for graceful shutdown                     │
│                                                     │
│ 2. DeleteContainerAsync(containerId)                │
│    - Remove container from system                   │
│    - Release container resources                    │
│                                                     │
│ 3. TempDirectory.Dispose()                          │
│    - Delete temp directory                          │
│    - Best-effort cleanup (ignore errors)            │
│                                                     │
│ 4. Fixture.Dispose()                                │
│    - Dispose ServiceProvider                        │
│    - Release DI resources                           │
└─────────────────────────────────────────────────────┘
					  ↓
				  Test Complete
```

This ensures:
- ✅ No resource leaks
- ✅ No orphaned containers
- ✅ Test isolation
- ✅ Graceful error handling
