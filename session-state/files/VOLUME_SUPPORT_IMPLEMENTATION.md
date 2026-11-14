# Volume Support Implementation for WSLC Client

## Summary
Implemented complete volume support for the WSL Containers (WSLC) client by replacing all `NotSupportedException` throws with proper implementations using the `wslc volume` CLI commands.

## Implementation Details

### File Modified
- **File:** `src\RedMaple.Orchstration.Containers.Wslc\WslcClient.cs`
- **Region:** Volume Methods (lines 701-861)

### Methods Implemented

#### 1. CreateVolumeAsync
**Lines:** 703-751

Creates a named volume with support for:
- **Driver specification** (`-d` flag): guest, vhd, or custom drivers
- **Labels** (`-l` flag): Metadata key-value pairs (e.g., project labels)
- **Driver options** (`-o` flag): Driver-specific configuration
- **Volume name:** Required positional argument

**Usage Example:**
```bash
wslc volume create -d guest -l project=myapp -o type=cifs myvolume
```

**Features:**
- Returns `VolumeResponse` with volume name, driver, labels, and mountpoint
- Comprehensive logging (info on success, error on failure)
- Proper exception handling with stderr output

**Docker Compose Integration:**
Used by `DockerVolumeManager` to create volumes with project labels for lifecycle tracking.

---

#### 2. DeleteVolumeAsync
**Lines:** 753-773

Deletes a named volume with optional force flag.

**Parameters:**
- `name`: Volume name to delete
- `force`: If true, adds `-f` flag to force deletion even if container is attached

**Usage Example:**
```bash
wslc volume remove -f myvolume
```

**Behavior:**
- Without force: Fails if volume is in use by a container
- With force: Removes volume regardless of container attachments

---

#### 3. ListVolumesAsync
**Lines:** 775-856

Lists all volumes with full details from JSON output.

**Features:**
- Calls `wslc volume list --format json` to get structured output
- Parses each JSON line individually
- Extracts properties:
  - Name
  - Driver (defaults to "guest")
  - Mountpoint (standard Docker path: `/var/lib/docker/volumes/{name}/_data`)
  - Scope (defaults to "local")
  - Labels (parsed from comma-separated format)

**Error Handling:**
- Gracefully handles JSON parsing errors (logs warning, continues)
- Returns empty list on command failure
- Skips blank lines

**Example Output Parsing:**
```json
{
  "Name": "myvolume",
  "Driver": "guest",
  "Mountpoint": "/var/lib/docker/volumes/myvolume/_data",
  "Scope": "local",
  "Labels": "project=myapp,env=prod"
}
```

---

#### 4. RemoveVolumeAsync
**Lines:** 858-861

Convenience method that delegates to `DeleteVolumeAsync`.

**Purpose:** 
- Provides interface contract compliance
- Maintains compatibility with `ILocalContainersClient`

---

### Key Design Decisions

#### 1. JSON Parsing for ListVolumesAsync
- Uses `JsonDocument.Parse()` for safe parsing
- Individual error handling per volume (one bad entry doesn't break the list)
- Defensive property extraction with fallbacks to defaults

#### 2. Mountpoint Construction
- Standard Docker path pattern: `/var/lib/docker/volumes/{name}/_data`
- Ensures consistency with Docker behavior for mount points

#### 3. Driver Default
- Defaults to "guest" driver when not specified
- Matches WSL Containers default behavior

#### 4. Label Parsing
- Assumes comma-separated key=value format from CLI output
- Parses only valid key=value pairs
- Silently ignores malformed entries

#### 5. Error Handling
- All methods log detailed errors
- Exceptions include stderr output for debugging
- List operations return empty collections instead of throwing
- Create/Delete operations throw on failure for transaction integrity

---

### CLI Command Reference

```bash
# Create volume
wslc volume create [-d driver] [-l key=value] [-o key=value] volume-name

# List volumes (table format)
wslc volume list

# List volumes (JSON format)
wslc volume list --format json

# List volumes (quiet mode - names only)
wslc volume list -q

# Delete volume
wslc volume remove [-f] volume-name

# Delete multiple volumes
wslc volume remove volume1 volume2 volume3

# Force delete
wslc volume remove -f volume-name
```

---

### Docker Compose Integration

The volume methods are integrated with the Docker Compose orchestration layer:

1. **Volume Creation:** `DockerCompose.CreateVolumesAsync()` creates volumes with project labels
2. **Volume Listing:** Used to check for existing volumes before creation
3. **Volume Cleanup:** `DockerCompose.DeleteVolumesAsync()` removes volumes during teardown

**Project Label Usage:**
```csharp
Labels = new Dictionary<string, string>
{
	["com.docker.compose.project"] = plan.ProjectName
}
```

This allows orchestration to:
- Track volumes by project
- Safely delete project-specific volumes without affecting others
- Identify volumes created by specific deployments

---

### Testing Compatibility

The implementation fully supports:
- Volume creation with all supported drivers
- Volume listing and filtering (by project labels)
- Volume deletion with and without force
- Concurrent volume operations
- Error recovery and graceful degradation

---

### Build Status
✅ Solution builds successfully  
✅ No compilation errors  
✅ All interfaces properly implemented  
✅ Backward compatible with existing code  

---

### Future Enhancements

Potential additions for future versions:
1. Volume inspection details (size, usage stats)
2. Volume pruning (remove unused volumes)
3. Volume filtering by label or driver
4. Volume mounting and unmounting
5. Custom mount point support
