# DockerCompose.Up Gap Analysis - Based on New/Updated Models

## 1. VOLUME GAPS

### Current Implementation (CreateVolumesAsync in DockerCompose.Up.cs, lines 65-89)
- ✓ Uses volume key as name (line 74)
- ✓ Respects volume.driver (line 75)
- ✓ Handles driver_opts (line 76)
- ✗ MISSING: Uses derived name instead of volume.name property
- ✗ MISSING: Ignores volume.external property
- ✗ MISSING: Doesn't include volume.labels

### Issue Details:

#### 1.1 Volume.name property NOT USED (NEW)
- **Specification**: "Sets a custom name for a volume. The name is used as is and not scoped with stack name"
- **Current Behavior**: Always uses dictionary key as volume name
- **Expected Behavior**: If volume.name is provided, use it instead of key
- **Impact**: Users cannot customize volume naming
- **Example**: 
  ```yaml
  volumes:
	mydb:
	  name: production-db
	  driver: local
  ```
  Should create volume named "production-db", not "mydb"

#### 1.2 Volume.external property NOT CHECKED (NEW)
- **Specification**: "If set to true, external specifies that this volume already exists on the platform"
- **Current Behavior**: Always tries to create the volume
- **Expected Behavior**: 
  - Skip creation if external: true
  - Validate that external volume exists before proceeding
  - Return error if external: true but volume doesn't exist
- **Impact**: Will fail with "Failed to create volume!" when external volumes don't exist
- **Example**:
  ```yaml
  volumes:
	backup_storage:
	  external: true
	  name: existing-backup-volume
  ```
  Should not attempt to create, should verify it exists

#### 1.3 Volume.labels NOT MERGED (NEW)
- **Specification**: "Add metadata to volumes using labels. You can use either an array or a dictionary"
- **Current Behavior**: Only adds project label
- **Expected Behavior**: Merge volume.labels with project label
- **Impact**: User-defined volume metadata is lost
- **Example**:
  ```yaml
  volumes:
	db_data:
	  labels:
		- "com.example.team=infra"
		- "com.example.purpose=backup"
  ```
  Should preserve these labels while adding project label

---

## 2. NETWORK GAPS

### Current Implementation (CreateNetworksAsync in DockerCompose.Networking.cs, lines 46-73)
- ✓ Uses network key as name (line 55)
- ✓ Respects network.driver (line 56)
- ✓ Handles network.Internal (line 57)
- ✓ Respects network.scope (line 58)
- ✗ MISSING: Uses derived name instead of network.name property
- ✗ MISSING: Ignores network.external property
- ✗ MISSING: Ignores network.ipam configuration
- ✗ MISSING: Ignores network.labels
- ✗ MISSING: Ignores network.attachable
- ✗ MISSING: Ignores network.driver_opts
- ✗ MISSING: Ignores network.enable_ipv4
- ✗ MISSING: Ignores network.enable_ipv6

### Issue Details:

#### 2.1 Network.name property NOT USED (NEW)
- **Specification**: "Sets a custom name for the network. The name is used as is and is not scoped with the project name"
- **Current Behavior**: Always uses dictionary key as network name
- **Expected Behavior**: If network.name is provided, use it instead of key
- **Impact**: Users cannot customize network naming
- **Example**:
  ```yaml
  networks:
	frontend:
	  name: public-network
	  driver: bridge
  ```
  Should create network named "public-network", not "frontend"

#### 2.2 Network.external property INCOMPLETE (NEW)
- **Specification**: "external specifies that this network's lifecycle is maintained outside of that of the application"
- **Current Behavior**: Attempts to create the network (no external check)
- **Expected Behavior**: 
  - Skip creation if external: true
  - Validate that external network exists
  - Return error if external: true but network doesn't exist
- **Impact**: Will fail when external networks don't exist
- **Example**:
  ```yaml
  networks:
	legacy_network:
	  external: true
	  name: existing-network
  ```
  Should not attempt to create, should verify it exists

#### 2.3 Network.ipam configuration COMPLETELY MISSING (NEW - CRITICAL)
- **Specification**: "Specifies a custom IPAM configuration for IP address management"
- **Current Behavior**: No IPAM settings are applied
- **Expected Behavior**: Pass IPAM configuration to Docker API including:
  - ipam.driver (IPAM driver name, e.g., "default")
  - ipam.config[] (per-network IPAM config):
	- subnet (e.g., "192.168.0.0/24")
	- ip_range (e.g., "192.168.0.1/28")
	- gateway (e.g., "192.168.0.1")
	- aux_addresses (auxiliary IPv4 or IPv6 addresses)
  - ipam.options (IPAM driver options)
- **Impact**: Custom IP ranges and gateways are ignored
- **Example**:
  ```yaml
  networks:
	backend:
	  driver: bridge
	  ipam:
		driver: default
		config:
		  - subnet: 172.20.0.0/16
			gateway: 172.20.0.1
			ip_range: 172.20.240.0/20
  ```
  Should configure specific subnet and gateway, not Docker defaults

#### 2.4 Network.labels NOT MERGED (NEW)
- **Specification**: "Add metadata to containers using labels. You can use either an array or a dictionary"
- **Current Behavior**: Only adds project label
- **Expected Behavior**: Merge network.labels with project label
- **Impact**: User-defined network metadata is lost
- **Example**:
  ```yaml
  networks:
	app:
	  labels:
		com.example.team: "platform"
		com.example.environment: "staging"
  ```
  Should preserve these labels while adding project label

#### 2.5 Network.attachable NOT USED (NEW)
- **Specification**: "If attachable is set to true, then standalone containers should be able to attach to this network, in addition to services"
- **Current Behavior**: Network created without attachable flag
- **Expected Behavior**: Pass attachable flag to Docker API
- **Impact**: Standalone containers cannot attach to user-managed networks
- **Example**:
  ```yaml
  networks:
	tools:
	  driver: bridge
	  attachable: true
  ```
  Should allow external containers to join

#### 2.6 Network.driver_opts NOT USED (NEW)
- **Specification**: "Specifies a list of options as key-value pairs to pass to the driver. These options are driver-dependent"
- **Current Behavior**: Driver options are completely ignored
- **Expected Behavior**: Pass network.driver_opts to Docker API
- **Impact**: Driver-specific configurations cannot be applied
- **Example**:
  ```yaml
  networks:
	custom:
	  driver: bridge
	  driver_opts:
		com.docker.network.bridge.name: br_custom
		com.docker.network.bridge.enable_icc: "false"
  ```
  Should apply these bridge-specific options

#### 2.7 Network.enable_ipv4 / enable_ipv6 NOT USED (NEW)
- **Specification**: 
  - enable_ipv4: "Can be used to disable IPv4 address assignment (Compose v2.33.1+)"
  - enable_ipv6: "Enables IPv6 address assignment"
- **Current Behavior**: No IP version control
- **Expected Behavior**: Pass enable_ipv4/enable_ipv6 flags to Docker API
- **Impact**: Cannot control which IP versions are used
- **Example**:
  ```yaml
  networks:
	ipv6_only:
	  driver: bridge
	  enable_ipv4: false
	  enable_ipv6: true
  ```
  Should disable IPv4 and enable IPv6

---

## 3. PRIORITY RANKING

### 🔴 CRITICAL (Breaks expected behavior, prevents features):
1. **Network.ipam** - IPAM configuration is fundamental for custom IP ranges/gateways
2. **Network.external** - Currently will try to create external networks instead of using existing
3. **Volume.external** - Will fail trying to create volumes that should already exist

### 🟠 HIGH (Incomplete feature implementation):
4. **Network.labels / Volume.labels** - Metadata loss, reduces observability
5. **Network.name / Volume.name** - Custom naming not honored
6. **Network.driver_opts** - Driver-specific options ignored

### 🟡 MEDIUM (Optional features, quality of life):
7. **Network.attachable** - Feature loss for standalone containers
8. **Network.enable_ipv4 / enable_ipv6** - IP version control not available

---

## 4. IMPLEMENTATION NOTES

### Docker.DotNet API Assumptions
Based on Docker SDK patterns, these parameters likely exist:
- **NetworksCreateParameters** should support:
  - Name ✓ (currently used)
  - Driver ✓ (currently used)
  - Internal ✓ (currently used)
  - Scope ✓ (currently used)
  - DriverOpts (needs verification)
  - Labels ✓ (currently used, needs merging)
  - Attachable (needs verification)
  - IPAM (needs verification - may be IPAM field or similar)
  - EnableIPv4/EnableIPv6 (needs verification)

- **VolumesCreateParameters** should support:
  - Name ✓ (currently used)
  - Driver ✓ (currently used)
  - DriverOpts ✓ (currently used)
  - Labels ✓ (currently used, needs merging)
  - External (needs verification)

### Suggested Implementation Order (by impact):
1. ✓ Add network.external check (prevents failed creation attempts) - CRITICAL
2. ✓ Add network.name usage (straightforward rename) - HIGH
3. ✓ Add volume.external check - CRITICAL
4. ✓ Add volume.name usage - HIGH
5. ✓ Add labels merging for both networks and volumes - HIGH
6. ✓ Add network.driver_opts for networks - HIGH
7. ✓ Add IPAM configuration (most complex) - CRITICAL
8. ✓ Add network.attachable flag - MEDIUM
9. ✓ Add network.enable_ipv4/enable_ipv6 flags - MEDIUM

---

## 5. FILES AFFECTED

### Requiring Changes:
1. **src/RedMaple.Orchestrator.DockerCompose/DockerCompose.Up.cs**
   - `CreateVolumesAsync()` method (lines 65-89)
   - Needs to handle: external, name, labels

2. **src/RedMaple.Orchestrator.DockerCompose/DockerCompose.Networking.cs**
   - `CreateNetworksAsync()` method (lines 46-73)
   - Needs to handle: external, name, labels, driver_opts, ipam, attachable, enable_ipv4, enable_ipv6

### Models (Already Complete):
- ✓ src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetwork.cs
- ✓ src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkIpam.cs
- ✓ src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkIpamConfig.cs
- ✓ src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeVolume.cs
- ✓ src/RedMaple.Orchestrator.DockerCompose/Models/DockerComposeNetworkDriverOpts.cs

---

## Summary

The Docker Compose models have been comprehensively updated to support the full specification, but the implementation code in `DockerCompose.Up.cs` and `DockerCompose.Networking.cs` has significant gaps. These gaps prevent full utilization of the new model properties, particularly:

- **Custom naming** (name property) not honored for networks and volumes
- **External resource management** (external property) not implemented
- **IP configuration** (ipam) not passed to Docker API
- **Metadata** (labels) not fully merged
- **Driver options** (driver_opts) and other configuration not applied

These gaps should be addressed to fully implement the Docker Compose specification.
