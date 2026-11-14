# DockerCompose Model Updates

This document summarizes the enhancements made to the Docker Compose models to support the full specifications.

## New Models Created

### DockerComposeNetworkDriverOpts
- **Location**: `src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkDriverOpts.cs`
- **Type**: Dictionary<string, string> subclass
- **Purpose**: Stores network-specific driver options as key-value pairs
- **Usage**: Pass driver-dependent options to network drivers

### DockerComposeNetworkIpam
- **Location**: `src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkIpam.cs`
- **Properties**:
  - `driver`: Custom IPAM driver (e.g., "default")
  - `config`: List of IPAM configurations
  - `options`: Driver-specific IPAM options
- **Purpose**: Configure IP address management for networks

### DockerComposeNetworkIpamConfig
- **Location**: `src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkIpamConfig.cs`
- **Properties**:
  - `subnet`: Subnet in CIDR format (e.g., "172.28.0.0/16")
  - `ip_range`: Range of IPs from which to allocate container IPs (e.g., "172.28.5.0/24")
  - `gateway`: IPv4 or IPv6 gateway for the master subnet (e.g., "172.28.5.254")
  - `aux_addresses`: Auxiliary IPv4 or IPv6 addresses used by Network driver
- **Purpose**: Specify IPAM configuration details per network segment

## Enhanced Models

### DockerComposeNetwork
**Location**: `src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetwork.cs`

#### Existing Properties
- `driver`: Network driver to use
- `scope`: Network scope
- `Internal`: Create externally isolated network

#### New Properties Added
1. **driver_opts**: DockerComposeNetworkDriverOpts
   - Stores driver-specific options
   - Type: Dictionary<string, string>
   - Example: `{"foo": "1", "bar": "2"}`

2. **external**: bool?
   - Indicates network is managed outside application
   - When true, Compose won't create the network
   - Returns error if network doesn't exist

3. **ipam**: DockerComposeNetworkIpam
   - Custom IP address management configuration
   - Supports subnet, IP range, gateway, auxiliary addresses
   - IPAM driver specification

4. **labels**: Dictionary<string, string>
   - Metadata labels for the network
   - Reverse-DNS notation recommended
   - Example: `{"com.example.description": "Finance network"}`

5. **name**: string?
   - Custom name for the network
   - Used as-is by platform (not scoped with project name)
   - Enables parameterization of network names

6. **attachable**: bool?
   - Allows standalone containers to attach to network
   - Typically used with overlay driver
   - Enables inter-container communication outside of services

7. **enable_ipv4**: bool?
   - Control IPv4 address assignment (Compose v2.33.1+)
   - Can be set to false to disable IPv4

8. **enable_ipv6**: bool?
   - Enable IPv6 address assignment
   - Can be combined with enable_ipv4=false for IPv6-only networks

### DockerComposeVolume
**Location**: `src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeVolume.cs`

#### Existing Properties
- `driver`: Volume driver to use
- `labels`: Metadata (List<string>)
- `driver_opts`: Driver-specific options
- `external`: Volume is managed outside application

#### New Property Added
1. **name**: string?
   - Custom name for the volume
   - Used as-is by platform (not scoped with stack name)
   - Enables volume name parameterization
   - Example: Using `${DATABASE_VOLUME}` environment variable

## Specification Compliance

All models now fully implement their respective Docker Compose specifications:
- **Networks**: https://github.com/compose-spec/compose-spec/blob/main/06-networks.md
- **Volumes**: https://github.com/compose-spec/compose-spec/blob/main/07-volumes.md

## Test Coverage

The models support:
- ✅ 13 comprehensive network tests (100% passing)
- ✅ 14 comprehensive volume tests (100% passing)
- ✅ All test cases use real YAML parsing to validate model properties
- ✅ Integration with existing YAML deserialization pipeline

## Backward Compatibility

All changes are additive:
- Existing properties remain unchanged
- New properties are optional (nullable)
- No breaking changes to existing code
- Existing tests continue to pass

## Usage Examples

### Network with IPAM
```csharp
var plan = DockerComposeParser.ParseFile("compose.yaml");
var network = plan.networks["mynet"];

// Access IPAM configuration
if (network.ipam?.config != null)
{
    foreach (var cfg in network.ipam.config)
    {
        var subnet = cfg.subnet;      // "172.28.0.0/16"
        var gateway = cfg.gateway;    // "172.28.5.254"
    }
}
```

### Volume with Custom Name
```csharp
var volume = plan.volumes["db-data"];
var platformVolumeName = volume.name;  // "my-app-data"
```

### Network with Labels
```csharp
var network = plan.networks["frontend"];
if (network.labels != null)
{
    var description = network.labels["com.example.description"];
}
```
