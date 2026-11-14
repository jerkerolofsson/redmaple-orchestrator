# Quick Reference - Running Docker Container Integration Tests

## ✅ What Was Implemented

5 comprehensive integration tests for Docker/Podman/WslContainers:

1. **CreateContainer_IsSuccessful** - Basic container creation & listing
2. **CreateAndDeleteContainer_ContainerIsRemovedFromListing** - Deletion verification
3. **CreateContainer_WithVolumeMount_IsSuccessful** - Volume mount binding
4. **CreateContainer_WithVolumeMount_ExecAndVerifyFile** - Full volume roundtrip
5. **CreateContainer_WithPortMapping_IsSuccessful** - Port mapping & HTTP connectivity

**Backend Coverage:** Docker, Podman, WslContainers × 5 tests = **15 total tests**

---

## 🚀 Running Tests

### Visual Studio Test Explorer (Easiest)
```
View → Test Explorer
Search: "CreateTests"
Click "Run" or "Run All"
```

### PowerShell Command Line
```powershell
cd C:\local\code\redmaple-orchestrator

# Run all CreateTests
dotnet test src\RedMaple.Orchestration.Containers.IntegrationTests\RedMaple.Orchestration.Containers.IntegrationTests.csproj --filter CreateTests

# Run specific backend
dotnet test --filter "CreateTests AND (Docker)"
dotnet test --filter "CreateTests AND (Podman)"
dotnet test --filter "CreateTests AND (WslContainers)"

# Run specific test
dotnet test --filter "CreateContainer_IsSuccessful"
```

---

## 📋 Test Categories

### Container Lifecycle Tests
- ✅ Create container with name
- ✅ List containers
- ✅ Delete container
- ✅ Verify deletion

### Volume Mount Tests  
- ✅ Create container with bind mount
- ✅ Start container
- ✅ Execute command in container
- ✅ Write to mounted volume
- ✅ Verify file on host

### Port Mapping Tests
- ✅ Create container with port bindings
- ✅ Start container  
- ✅ Verify port mapping works
- ✅ HTTP connectivity test

---

## 📦 Key Code Changes

### TempDirectory Enhancement
```csharp
using var tempDir = new TempDirectory();
var hostPath = tempDir.Path;  // ← Now accessible!
```

### Container Creation with Volumes
```csharp
var response = await client.CreateContainerAsync(new CreateContainerParameters
{
	Image = "alpine:latest",
	Name = "test-container",
	Volumes = new Dictionary<string, EmptyStruct> { { "/volume", default } },
	HostConfig = new HostConfig
	{
		Binds = new List<string> { $"{hostPath}:/volume" }
	}
}, default);
```

### Container Creation with Ports
```csharp
var response = await client.CreateContainerAsync(new CreateContainerParameters
{
	Image = "registry:2",
	ExposedPorts = new Dictionary<string, EmptyStruct> { { "5000/tcp", default } },
	HostConfig = new HostConfig
	{
		PortBindings = new Dictionary<string, IList<PortBinding>>
		{
			{ "5000/tcp", new List<PortBinding> { new PortBinding { HostPort = "5555" } } }
		}
	}
}, default);
```

---

## ⚙️ Prerequisites

- ✅ Docker daemon running (for Docker tests)
- ✅ Podman daemon running (for Podman tests)  
- ✅ WSL Containers (wslc) installed (for WslContainers tests)
- ✅ Internet connection (for initial image pulls)
- ✅ Port 5555 available (for port mapping test)

---

## 🐛 Troubleshooting

| Issue | Fix |
|-------|-----|
| "Docker API responded with status code=NotFound" | Internet needed for alpine:latest pull |
| "Failed to create container: Invalid volume specifications" | Use WslcClient 2.0+ (updated) |
| "The request was canceled" in port mapping test | Increase delay or check registry startup |
| Tests timeout on WslContainers | May need 1000+ ms delays |
| "No such image" on second run | Images are cached, should work after first pull |

---

## 📊 Expected Results

```
Test run completed. Ran 15 test(s).
✅ 15 Passed, ❌ 0 Failed, ⏭️  0 Skipped
```

**Estimated duration:** 60-90 seconds for full suite

---

## 🔍 Test Output Details

### Successful Test Flow
```
[INFO] Creating container with image alpine:latest
[INFO] Container created: abc123def456...
[PASS] CreateContainer_IsSuccessful
[INFO] Deleting container abc123def456
[PASS] Cleanup complete
```

### Volume Test Flow
```
[INFO] Creating container with volume mount
[INFO] Starting container
[INFO] Executing: sh -c "echo ',,,' > /volume/test.txt"
[PASS] File exists at host path
[PASS] File content verified
[INFO] Cleaning up
```

### Port Mapping Test Flow
```
[INFO] Creating registry:2 container
[INFO] Port mapping: 5555:5000
[INFO] Starting container
[INFO] Waiting for registry startup (2000ms)
[INFO] Testing HTTP: GET http://localhost:5555/v2/
[PASS] Response: 200 OK (or 401 Unauthorized)
[INFO] Port mapping verified
```

---

## 💡 Tips

1. **First run will be slow** - Images are pulled automatically
2. **Use specific backend** - Run individual backends to debug faster
3. **Check container logs** - Useful if tests fail mysteriously
4. **Port cleanup** - If port 5555 is in use, tests may timeout
5. **Temp cleanup** - TempDirectory auto-deletes; no manual cleanup needed

---

## 📝 Files Modified

1. **TempDirectory.cs** - Added Path property, improved cleanup
2. **CreateTests.cs** - 5 new comprehensive tests
3. **WslcClient.cs** - Fixed volume and port binding parsing

**No breaking changes** - All existing tests still pass.
