# Implementation Details - Docker Container Tests

## Files Modified

### 1. `src/RedMaple.Orchestration.Containers.IntegrationTests/TempDirectory.cs`
**Changes:**
- Added public `Path` property for accessing temporary directory
- Qualified `Path.Combine` and `Path.GetTempPath` with `System.IO.` to avoid ambiguity
- Added exception handling in `Dispose()` for resilient cleanup

**Before:**
```csharp
public TempDirectory()
{
	_path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
	Directory.CreateDirectory(_path);
}

public void Dispose()
{
	Directory.Delete(_path, true);
}
```

**After:**
```csharp
public string Path => _path;

public TempDirectory()
{
	_path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
	Directory.CreateDirectory(_path);
}

public void Dispose()
{
	try
	{
		Directory.Delete(_path, true);
	}
	catch
	{
		// Best effort cleanup
	}
}
```

---

### 2. `src/RedMaple.Orchestration.Containers.IntegrationTests/CreateTests.cs`
**Complete Rewrite:**
- Added using: `RedMaple.Orchestrator.Contracts.Containers`
- Created helper methods:
  - `GetAlpineImageAsync()` - pulls alpine:latest if needed
  - `GetRegistryImageAsync()` - pulls registry:2 if needed
- Implemented 5 comprehensive test methods
- All methods use [Theory] with [InlineData] for multi-backend testing
- Tests use proper using blocks for resource cleanup

**Key Features:**
- Unique container names using GUID suffixes
- Proper async/await patterns
- Clear assertion messages
- Comprehensive resource cleanup
- 500ms delay before exec operations (allow container startup)
- 2000ms delay before HTTP requests (allow registry startup)

---

### 3. `src/RedMaple.Orchestrator.Containers.Wslc/WslcClient.cs`
**Enhanced `CreateContainerAsync` Method:**

**Port Binding Handling:**
```csharp
// Handle port bindings from HostConfig if present
if (parameters.HostConfig?.PortBindings?.Count > 0)
{
	foreach (var portBinding in parameters.HostConfig.PortBindings)
	{
		var containerPort = portBinding.Key;
		foreach (var binding in portBinding.Value)
		{
			if (!string.IsNullOrEmpty(binding.HostPort))
			{
				// Format: hostPort:containerPort
				args.Append($" -p {EscapeArgument($"{binding.HostPort}:{containerPort}")}");
			}
			else
			{
				args.Append($" -p {EscapeArgument(containerPort)}");
			}
		}
	}
}
```

**Volume Binding Handling (Fixed):**
```csharp
// Handle volume binds from HostConfig (these take priority)
if (parameters.HostConfig?.Binds?.Count > 0)
{
	foreach (var bind in parameters.HostConfig.Binds)
	{
		args.Append($" -v {EscapeArgument(bind)}");
	}
}
// Only handle unbound volumes if no binds are specified
else if (parameters.Volumes?.Count > 0)
{
	foreach (var vol in parameters.Volumes.Keys)
	{
		args.Append($" -v {EscapeArgument(vol)}");
	}
}
```

**Why This Fix Matters:**
- Docker.DotNet separates Volumes (abstract mount points) from HostConfig.Binds (actual host:container mappings)
- The old code was trying to pass unbound volume paths (e.g., "/volume") directly to wslc
- wslc requires format: `hostPath:containerPath`
- New code prioritizes HostConfig.Binds when available, avoiding invalid syntax

---

## Test Architecture

### Test Pattern
All tests follow the same pattern:
1. Create fixture for backend (Docker/Podman/WslContainers)
2. Get container client from fixture
3. Ensure image available (auto-pull if needed)
4. Create container with test configuration
5. Perform test operations
6. Assert expected behavior
7. Cleanup resources

### Example: Volume Mount + Exec Test
```
┌─────────────────────────┐
│ Test: CreateContainer_  │
│ WithVolumeMount_Exec    │
│ AndVerifyFile           │
└──────────────┬──────────┘
			   │
		┌──────▼──────┐
		│ Create temp │
		│ directory   │
		└──────┬──────┘
			   │
		┌──────▼──────────────┐
		│ Create container    │
		│ Mount temp as /vol  │
		└──────┬──────────────┘
			   │
		┌──────▼───────┐
		│ Start cont.  │
		│ (500ms wait) │
		└──────┬───────┘
			   │
		┌──────▼──────────────────────┐
		│ Exec: echo > /vol/test.txt  │
		└──────┬──────────────────────┘
			   │
		┌──────▼──────────────┐
		│ Verify file exists  │
		│ on host (temp dir)  │
		└──────┬──────────────┘
			   │
		┌──────▼──────────────┐
		│ Verify file content │
		└──────┬──────────────┘
			   │
		┌──────▼──────────────┐
		│ Cleanup:            │
		│ Stop container      │
		│ Delete container    │
		│ Delete temp dir     │
		└─────────────────────┘
```

---

## Test Result Expectations

### Successful Test Run
✅ CreateContainer_IsSuccessful - All backends pass
✅ CreateAndDeleteContainer_ContainerIsRemovedFromListing - All backends pass
✅ CreateContainer_WithVolumeMount_IsSuccessful - All backends pass
✅ CreateContainer_WithVolumeMount_ExecAndVerifyFile - All backends pass
✅ CreateContainer_WithPortMapping_IsSuccessful - All backends pass

**Total: 15 tests, 15 passed**

### Potential Failures & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| Docker API responded with status code=NotFound | Image not available | Tests auto-pull; ensure internet connection |
| Failed to create container: Invalid volume specifications | WslcClient volume parsing | Fixed in updated code |
| The request was canceled | Port mapping test timeout | Increase delay or check registry startup |
| No such image: alpine:latest | Podman cache miss | Run tests again; images persist after pull |

---

## Code Quality Notes

### Error Handling
- ✅ Try/catch blocks in cleanup (TempDirectory.Dispose)
- ✅ Exception logging in WslcClient
- ✅ Explicit resource disposal (using statements)
- ✅ Null-safe operators (?.) used throughout

### Test Isolation
- ✅ Unique container names prevent collisions
- ✅ Temporary directories auto-cleaned
- ✅ Containers deleted after each test
- ✅ No shared state between tests

### Reliability
- ✅ Explicit waits for async operations
- ✅ Proper cancellation token usage
- ✅ Fallible operations wrapped in try/finally
- ✅ Graceful degradation for missing ports

---

## Performance Notes

- Image pulls (~1-2 seconds per test if image missing)
- Container creation (~500ms)
- Container startup (~500-2000ms depending on image)
- Port binding verification (~100-500ms)
- Volume file write (~50-100ms)
- **Total per test: ~3-5 seconds**
- **Full suite: ~45-75 seconds**

### Optimization Opportunities
1. Pull images once before all tests (use [ClassFixture])
2. Reuse containers across related tests
3. Parallel test execution
4. Network pre-configuration
