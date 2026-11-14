# Code Update Requirements - DockerCompose.Up Implementation

## File 1: DockerCompose.Up.cs - CreateVolumesAsync Method

### CURRENT CODE (Lines 65-89)
```csharp
private async Task CreateVolumesAsync(DockerComposePlan plan, CancellationToken cancellationToken)
{
	foreach(var pair in plan.volumes)
	{
		var name = pair.Key;
		DockerComposeVolume composeVolume = pair.Value;

		var volumeCreateParameters = new VolumesCreateParameters
		{
			Name = name,                                    // ← ISSUE 1: Should use volume.name if available
			Driver = composeVolume?.driver ?? "local",
			DriverOpts = composeVolume?.driver_opts,
			Labels = new Dictionary<string, string>
			{
				[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
														// ← ISSUE 2: Should merge volume.labels here
			}
		};

		var createVolumeResult = await _volumes.TryCreateVolumeAsync(volumeCreateParameters, cancellationToken);
		if(createVolumeResult == CreateVolumeResult.Error)
		{
			throw new Exception("Failed to create volume!");
		}
	}
}
```

### REQUIRED CHANGES

#### Change 1: Handle volume.external property
**Before creation attempt, check if volume is external:**
```csharp
if (composeVolume?.external == true)
{
	// For external volumes, validate they exist instead of creating
	_logger.LogInformation("Volume {VolumeName} marked as external, validating existence..", name);
	// TODO: Add validation logic to check if volume exists
	continue;
}
```

#### Change 2: Use volume.name if provided
**Replace line 74:**
```csharp
// OLD: Name = name,
// NEW:
Name = composeVolume?.name ?? name,  // Use custom name if provided, fallback to key
```

#### Change 3: Merge volume.labels
**Replace labels dictionary creation:**
```csharp
// OLD:
Labels = new Dictionary<string, string>
{
	[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
}

// NEW:
Labels = MergeLabels(
	composeVolume?.labels?.ToDictionary(l => l.Split('=')[0], l => l.Split('=').Length > 1 ? l.Split('=')[1] : ""),
	new Dictionary<string, string>
	{
		[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
	}
)
```

---

## File 2: DockerCompose.Networking.cs - CreateNetworksAsync Method

### CURRENT CODE (Lines 46-73)
```csharp
private async Task<List<NetworkResponse>> CreateNetworksAsync(DockerComposePlan plan, CancellationToken cancellationToken)
{
	var ret = new List<NetworkResponse>();

	foreach (var pair in plan.networks)
	{
		var name = pair.Key;
		var network = pair.Value;
		var existingNetwork = await FindProjectNetwork(plan, name, cancellationToken);
		if(existingNetwork is null)
		{
			await _docker.CreateNetworkAsync(new NetworksCreateParameters
			{
				Name = name,                                    // ← ISSUE 1: Should use network.name if available
				Driver = network.driver,                       // ✓ OK
				Internal = network.Internal ?? true,           // ✓ OK
				Scope = network.scope,                         // ✓ OK
				// ← MISSING PROPERTIES (Issues 2-8):
				// - DriverOpts = network.driver_opts
				// - Attachable = network.attachable
				// - IPAM configuration
				// - EnableIPv4/EnableIPv6
				Labels = new Dictionary<string, string>
				{
					[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
															// ← Should merge network.labels
				}
			}, cancellationToken);

			_logger.LogInformation("Creating network {NetworkName}, driver={NetworkDriver}..", name, network.driver);
			ret.Add(await FindProjectNetwork(plan, name, cancellationToken) ?? throw new Exception("Failed to create network"));
		}
		else
		{
			_logger.LogInformation("Existing network {NetworkName} found..", name);
			ret.Add(existingNetwork);
		}
	}

	return ret;
}
```

### REQUIRED CHANGES

#### Change 1: Check network.external property
**Before creation attempt, add check:**
```csharp
if (network.external == true)
{
	_logger.LogInformation("Network {NetworkName} marked as external, validating existence..", name);
	// Validate external network exists
	var externalNetwork = await FindNetwork(plan, network.name ?? name, cancellationToken);
	if (externalNetwork is null)
	{
		throw new Exception($"External network {name} does not exist");
	}
	ret.Add(externalNetwork);
	continue;
}
```

#### Change 2: Use network.name if provided
**Replace line 55:**
```csharp
// OLD: Name = name,
// NEW:
Name = network.name ?? name,  // Use custom name if provided, fallback to key
```

#### Change 3: Add driver_opts
**Add to NetworksCreateParameters:**
```csharp
DriverOpts = network.driver_opts?.Count > 0 ? network.driver_opts : null,
```

#### Change 4: Add attachable flag
**Add to NetworksCreateParameters:**
```csharp
Attachable = network.attachable ?? false,
```

#### Change 5: Add IP version controls
**Add to NetworksCreateParameters:**
```csharp
// These may be part of IPAM config or separate fields
EnableIPv6 = network.enable_ipv6,
// Note: enable_ipv4 may not have a direct equivalent in NetworksCreateParameters
```

#### Change 6: Merge network.labels
**Replace labels dictionary creation:**
```csharp
// OLD:
Labels = new Dictionary<string, string>
{
	[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
}

// NEW:
Labels = MergeLabels(
	network.labels,
	new Dictionary<string, string>
	{
		[DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
	}
)
```

#### Change 7: Add IPAM configuration (MOST COMPLEX)
**Add to NetworksCreateParameters:**
```csharp
IPAM = network.ipam != null ? CreateIPAMConfiguration(network.ipam) : null,
```

**Then implement CreateIPAMConfiguration method:**
```csharp
private IPAM CreateIPAMConfiguration(DockerComposeNetworkIpam ipamConfig)
{
	var ipam = new IPAM
	{
		Driver = ipamConfig.driver ?? "default",
		Config = new List<IPAMConfig>(),
		Options = ipamConfig.options
	};

	if (ipamConfig.config is not null && ipamConfig.config.Count > 0)
	{
		foreach (var config in ipamConfig.config)
		{
			ipam.Config.Add(new IPAMConfig
			{
				Subnet = config.subnet,
				IPRange = config.ip_range,
				Gateway = config.gateway,
				AuxAddress = config.aux_addresses
			});
		}
	}

	return ipam;
}
```

---

## Required Helper Method (Both Files)

### MergeLabels Helper Method
Add to DockerCompose class (can be in either file or create new Helpers file):

```csharp
private Dictionary<string, string> MergeLabels(
	Dictionary<string, string>? customLabels, 
	Dictionary<string, string> defaultLabels)
{
	var result = new Dictionary<string, string>(defaultLabels);

	if (customLabels is not null)
	{
		foreach (var label in customLabels)
		{
			result[label.Key] = label.Value;
		}
	}

	return result;
}
```

---

## Testing Considerations

After implementing these changes, the following scenarios should be tested:

### Volume Tests
- [ ] External volume that exists → should not attempt creation
- [ ] External volume that doesn't exist → should throw error
- [ ] Volume with custom name → should create with custom name
- [ ] Volume with labels → should merge labels with project label
- [ ] Volume without external/name/labels → should work as before (backward compatibility)

### Network Tests
- [ ] External network that exists → should not attempt creation
- [ ] External network that doesn't exist → should throw error
- [ ] Network with custom name → should create with custom name
- [ ] Network with IPAM config → should apply subnet/gateway/IP range
- [ ] Network with driver_opts → should apply driver options
- [ ] Network with attachable: true → standalone containers can attach
- [ ] Network with labels → should merge labels with project label
- [ ] Network with enable_ipv6: true → should enable IPv6
- [ ] Network without new properties → should work as before (backward compatibility)

---

## Implementation Complexity Estimate

| Feature | Complexity | Effort | Risk |
|---------|-----------|--------|------|
| external checks | Low | 15 min | Low |
| name property usage | Low | 10 min | Low |
| labels merging | Low | 20 min | Low |
| driver_opts | Low | 10 min | Low |
| attachable flag | Low | 5 min | Low |
| enable_ipv6 flag | Low | 5 min | Low |
| IPAM configuration | High | 45 min | Medium |
| **Total** | - | **2 hours** | **Low-Medium** |

---

## API Verification Needed

Before implementation, verify Docker.DotNet API supports these properties:

```csharp
// Check NetworksCreateParameters properties
var networkParams = new NetworksCreateParameters();
// - DriverOpts property exists?
// - Attachable property exists?
// - IPAM property exists?
// - EnableIPv6 property exists?
// - Labels property works as dictionary? ✓ (already used)

// Check VolumesCreateParameters properties
var volumeParams = new VolumesCreateParameters();
// - External property exists?
// - Labels property works as dictionary? ✓ (already used)
```

If properties don't exist, may need to:
1. Update Docker.DotNet NuGet package to latest version
2. Use reflection or workarounds
3. Document limitations

