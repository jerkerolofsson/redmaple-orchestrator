# WSL Containers (WSLC) Implementation Analysis

## Research Summary

I analyzed the `nicknad/Wslc.Testcontainers` reference implementation to identify solutions for volumes, networks, and other features currently marked as `NotSupportedException` in our WslcClient.

### Key Findings

The reference implementation proves that **WSLC SDK provides robust support** for features we assumed were limited to CLI-only:

#### 1. **Volumes** ✅ FULLY SUPPORTED VIA SDK
- **Scratch Volumes (VHD-based)**: Created via `Session.CreateVhdVolume(VhdOptions)`
- **Host Mounts**: Passed to container via `ContainerSettings.Volumes` (host path → container path mappings)
- **Named Volumes**: Created and attached via `ContainerSettings.NamedVolumes`

Reference code:
```csharp
// Scratch VHD volumes
_session.CreateVhdVolume(new VhdOptions(
	volume.Name, 
	volume.SizeBytes, 
	ToSdkVhdType(volume.Type)
));

// Host volume mounts
settings.Volumes.Add(new ContainerVolume(
	volume.HostPath, 
	volume.ContainerPath, 
	volume.ReadOnly
));

// Named volumes
settings.NamedVolumes.Add(new ContainerNamedVolume(
	volume.Name, 
	volume.ContainerPath, 
	volume.ReadOnly
));
```

**Our current status**: Currently throws `NotSupportedException`. This should be removed and replaced with SDK calls.

#### 2. **Networking** ✅ FULLY SUPPORTED VIA SDK
- **Port Mapping**: `ContainerPortMapping` with dynamic/fixed host ports
- **Port Resolution**: Extracted from `Container.Inspect()` JSON payload
- **Network Modes**: `ContainerNetworkingMode.Bridged` (default) or `ContainerNetworkingMode.None` (isolated)
- **Bind Addresses**: Windows-specific port binding addresses via `WindowsAddress`

Reference code:
```csharp
// Port mapping with bind address
var mapping = new ContainerPortMapping(
	(ushort)hostPort, 
	(ushort)containerPort, 
	PortProtocol.TCP
);
if (bindAddress != null)
{
	mapping.WindowsAddress = new Windows.Networking.HostName(bindAddress);
}
settings.PortMappings.Add(mapping);

// Network mode
settings.NetworkingMode = networkMode == ContainerNetworkMode.Isolated 
	? ContainerNetworkingMode.None 
	: ContainerNetworkingMode.Bridged;
```

**Dynamic Port Resolution**: The reference uses polling (200 attempts @ 50ms = ~10s) to extract mapped ports from the inspect JSON:
- Container.Inspect() returns JSON with dynamic host ports
- Parsing looks for `Ports["containerPort/tcp"][0].HostPort`
- Cached for readiness probes and connection attempts

**Our current status**: Currently throws `NotSupportedException` for network creation. Container-level port mapping can be implemented via SDK.

#### 3. **Process Execution** ✅ SDK-NATIVE, NOT System.Diagnostics
The reference uses `Microsoft.WSL.Containers.Process` classes instead of `System.Diagnostics.Process`:

**Key findings**:
- `Container.CreateProcess(ProcessSettings)` returns `ContainerProcess` 
- `ContainerProcess.Start()` launches process inside container
- `Container.InitProcess` is the main container process (PID 1)
- Process output captured via `GetOutputStream(ProcessOutputHandle.StandardOutput)`
- Process stdin via `GetInputStream()`
- NO System.Diagnostics.Process used for container operations

**Benefits over CLI**:
- Direct SDK control without CLI parsing
- Output streaming via WinRT IInputStream/IOutputStream
- Better error handling with HResult exceptions
- No argument escaping issues
- Atomic file operations

**Our current status**: We're using `System.Diagnostics.Process` for wslc CLI commands. This is fine for CLI—the SDK classes are for *processes inside containers*. The reference uses SDK for Exec/CopyTo/CopyFrom operations.

#### 4. **Container Lifecycle** ✅ SDK OPERATIONS
- `Session.CreateContainer(ContainerSettings)` → creates container
- `Container.Start()` → starts container  
- `Container.Stop(Signal, TimeSpan)` → graceful shutdown
- `Container.Delete(DeleteContainerOption)` → remove container
- `Container.Inspect()` → JSON inspection payload

#### 5. **Image Handling** ✅ HYBRID APPROACH
The reference implementation:
- **Image pulling**: Uses a separate `WslImageResolver` (implementation-specific)
- **Local/Tarball images**: Loads from `configuration.TarballImageName`
- **Image caching**: Stored in session VHD at creation time
- **Our approach**: CLI for pulling, SDK container creation

#### 6. **Advanced Features**
- **Reuse instances**: Shared VHDs across container runs (configuration-based)
- **Storage management**: Per-instance or shared session storage
- **Metadata tracking**: Session ID, instance ID, owner PID, creation time
- **Logging**: Event-based log broadcasting with snapshot support
- **Copy operations**: Full file streaming between host and container with 1 GiB limits

---

## Recommendations for RedMaple.Orchestrator

### Priority 1: Immediate SDK Improvements (High Value, Low Risk)

#### A. **Implement Volumes via SDK**
Replace `GetVolumesAsync()` and `CreateVolumeAsync()` NotSupportedException with SDK calls:

```csharp
public async Task<CreateVolumeResponse> CreateVolumeAsync(
	string? name = null, 
	IReadOnlyDictionary<string, string>? labels = null,
	IReadOnlyDictionary<string, string>? options = null,
	CancellationToken cancellationToken = default)
{
	name ??= $"vol_{Guid.NewGuid():N}";

	var vhdOptions = new VhdOptions(
		name,
		sizeBytes: long.Parse(options?["size"] ?? "1073741824"), // 1 GB default
		type: options?["type"] == "fixed" ? VhdType.Fixed : VhdType.Dynamic
	);

	_session.CreateVhdVolume(vhdOptions);

	return new CreateVolumeResponse { Name = name };
}
```

**Benefits**: 
- Scratch volume support on WSLC
- Consistent API with Docker/Podman

#### B. **Implement Port Mapping Resolution**
Enhance port mapping to resolve dynamic assignments from Container.Inspect():

```csharp
private async Task<int> ResolvePortMappingAsync(int containerPort, CancellationToken ct)
{
	const int maxAttempts = 200;
	for (var i = 0; i < maxAttempts; i++)
	{
		var inspectJson = _container.Inspect();
		if (TryExtractMappedPort(inspectJson, containerPort, out var hostPort))
			return hostPort;

		await Task.Delay(50, ct);
	}
	throw new InvalidOperationException($"Port {containerPort} mapping timeout");
}

private static bool TryExtractMappedPort(string json, int containerPort, out int hostPort)
{
	var doc = JsonDocument.Parse(json);
	if (doc.RootElement.TryGetProperty("Ports", out var ports))
	{
		var key = $"{containerPort}/tcp";
		if (ports.TryGetProperty(key, out var mapping))
		{
			var items = mapping.EnumerateArray().ToList();
			if (items.Any() && items[0].TryGetProperty("HostPort", out var hostPortElem))
			{
				hostPort = int.Parse(hostPortElem.GetString() ?? "0");
				return hostPort > 0;
			}
		}
	}
	hostPort = 0;
	return false;
}
```

#### C. **Implement Process Execution via SDK**
Switch from wslc CLI to SDK Process API for Exec operations:

```csharp
public async Task<ExecResponse> ExecAsync(
	string id,
	IList<string> cmd,
	bool attachStdout = true,
	bool attachStderr = true,
	CancellationToken cancellationToken = default)
{
	var container = GetContainerByIdOrName(id);

	var settings = new ProcessSettings
	{
		CommandLine = cmd,
		OutputMode = ProcessOutputMode.Event
	};

	var process = container.CreateProcess(settings);
	process.Start();

	var stdout = await process.GetOutputStream(ProcessOutputHandle.StandardOutput)
		.AsStreamForRead()
		.ReadToEndAsync(cancellationToken);

	await process.WaitForExitAsync();

	return new ExecResponse
	{
		ExitCode = process.ExitCode,
		Output = Encoding.UTF8.GetString(stdout)
	};
}
```

### Priority 2: Medium-Term Enhancements (Good Value, More Complexity)

#### D. **Session Management**
Currently we create containers per ILocalContainersClient. Consider managing sessions:

```csharp
private Session? _session;

private Session GetOrCreateSession()
{
	if (_session != null) return _session;

	var settings = new SessionSettings(_instanceName, _storagePath)
	{
		MemorySizeInMB = _configuration.MemoryMb,
		CpuCount = _configuration.CpuCount
	};

	_session = new Session(settings);
	_session.Start();
	return _session;
}
```

#### E. **Container Inspection Integration**
Store and use Container.Inspect() data for GetContainers:

```csharp
public async Task<IList<ContainerListResponse>> GetContainersAsync(
	bool all = false, 
	CancellationToken cancellationToken = default)
{
	if (_container == null) return new List<ContainerListResponse>();

	var inspectJson = _container.Inspect();
	var doc = JsonDocument.Parse(inspectJson);

	return new[]
	{
		new ContainerListResponse
		{
			ID = doc.RootElement.GetProperty("Id").GetString(),
			Image = doc.RootElement.GetProperty("Image").GetString(),
			State = doc.RootElement.GetProperty("State").GetString(),
			Status = doc.RootElement.GetProperty("Status").GetString(),
			// ... map remaining properties
		}
	};
}
```

### Priority 3: Future Enhancements (Lower Priority)

- **Network creation**: Currently CLI-only; WSLC SDK may not support custom networks
- **Stats collection**: WinRT performance APIs might offer container metrics
- **Image building**: Not available in current SDK; CLI `wslc image build` could work
- **Volume snapshots**: Potential VHD snapshot functionality

---

## Implementation Strategy

### Phase 1: Volumes (1-2 hours)
1. Store `Session` reference in WslcClient
2. Implement `GetVolumesAsync()` using `Session.ListVhdVolumes()` if available, else return empty
3. Implement `CreateVolumeAsync()` using `Session.CreateVhdVolume()`
4. Implement `DeleteVolumeAsync()` using `Session.DeleteVhdVolume()`
5. Update container creation to pass volumes via `ContainerSettings.Volumes` and `ContainerSettings.NamedVolumes`

### Phase 2: Port Mapping (1-2 hours)
1. Enhance port mapping in `CreateContainerAsync()` to use `ContainerPortMapping`
2. Implement dynamic port resolution polling in `StartAsync()`
3. Update `GetContainerByIdAsync()` to include port information
4. Add port wait strategy (TCP connection probe)

### Phase 3: Process Execution (2-3 hours)
1. Refactor `ExecAsync()` to use SDK `Container.CreateProcess()` instead of wslc CLI
2. Stream stdout/stderr via WinRT IInputStream/IOutputStream
3. Handle process signals and timeouts
4. Update logging to use SDK event model

### Phase 4: Testing & Validation
1. Unit tests for volume operations
2. Integration tests for port mapping with actual containers
3. Process execution parity tests (CLI vs SDK)
4. Performance benchmarks vs Docker/Podman

---

## Risk Assessment

| Feature | Risk | Mitigation |
|---------|------|-----------|
| Volumes | LOW | SDK is designed for this; reference impl proves it works |
| Port Mapping | LOW | Parsing JSON from `Inspect()` is straightforward |
| Process Execution | LOW-MED | SDK API differs from CLI; needs careful integration testing |
| Session Reuse | MED | Reference uses VHD reuse; our single-instance model is simpler |
| File Copy | MED | WinRT stream handling requires async coordination |

---

## What About Microsoft.WSL.Containers.Process?

**Short answer**: It's for processes *inside* containers, not host processes.

**Details**:
- `Microsoft.WSL.Containers.Process` is the WinRT representation of a container process
- Returned by `Container.CreateProcess()` and accessible as `Container.InitProcess`
- Replaces System.Diagnostics.Process for *container operations*
- Our current System.Diagnostics.Process usage is correct—we're running wslc CLI on the host
- The reference implementation uses it via `process.NativeProcess.GetInputStream()` etc.

**Migration opportunity**: We could replace wslc CLI calls with SDK `Container.Exec()`-like operations, but the reference shows most WSLC work is still done via CLI, with SDK used for container lifecycle only.

---

## Conclusion

**The current "very limited" assessment was premature.** The WSLC SDK provides:
- ✅ Full volume support (VHD-based scratch volumes + host mounts)
- ✅ Complete networking (port mapping with dynamic resolution)
- ✅ Process execution (SDK Process API with streaming I/O)
- ✅ Container inspection (JSON payloads for querying state)

**Recommended approach**:
1. **Keep CLI integration** for simple list/inspect operations
2. **Adopt SDK for**:
   - Volume creation and management
   - Container configuration (ports, volumes, mounts)
   - Exec operations within containers
   - Process streaming I/O
3. **Maintain Docker.DotNet.Models compatibility** by mapping SDK types to contract types

**Next steps**: Implement Phases 1-2 (volumes + port mapping) to unlock the full feature set that Docker and Podman have.
