# Example Test Execution & Output

## Test Execution Flow

### Test 1: CreateContainer_IsSuccessful

#### What it does:
```csharp
[Theory]
[InlineData(ContainerBackend.Docker)]
[InlineData(ContainerBackend.Podman)]
[InlineData(ContainerBackend.WslContainers)]
public async Task CreateContainer_IsSuccessful(ContainerBackend backend)
{
	// 1. Create fixture for backend
	using var fixture = new LocalContainerBackendFixture(backend);
	var client = fixture.LocalContainersClient;

	// 2. Get image (auto-pull if needed)
	var image = await GetAlpineImageAsync(client);

	// 3. Create container with unique name
	var containerName = $"test-create-{Guid.NewGuid().ToString()[..8]}";
	var response = await client.CreateContainerAsync(new CreateContainerParameters
	{
		Image = image,
		Name = containerName,
		Cmd = new List<string> { "sleep", "1000" }
	}, default);

	// 4. Assertions
	Assert.NotNull(response);
	Assert.NotNull(response.ID);

	// 5. Verify in listing
	var containers = await client.GetContainersAsync(null, default);
	var created = containers.FirstOrDefault(c => c.Id == response.ID);
	Assert.NotNull(created);
	Assert.Equal(containerName, created.Name);

	// 6. Cleanup
	await client.DeleteContainerAsync(response.ID, default);
}
```

#### Expected Output:
```
[xUnit.net 00:00:05.23] test-create-a1b2c3d4
[xUnit.net 00:00:05.45] RedMaple.Orchestration.Containers.IntegrationTests.CreateTests.CreateContainer_IsSuccessful(backend: Docker) [PASS]
```

---

### Test 2: CreateContainer_WithVolumeMount_ExecAndVerifyFile

#### Detailed Flow:

**Setup Phase:**
```
1. Create temporary directory: C:\Temp\a1b2c3d4\
2. Create container:
   - Name: test-exec-e5f6g7h8
   - Image: alpine:latest
   - Volume: C:\Temp\a1b2c3d4\ → /volume
3. Container state: created but not running
```

**Execution Phase:**
```
4. Start container
   - Container initializes and reaches ready state
   - Alpine sleep 1000 running in background

5. Execute command in container:
   - Command: sh -c "echo ',,,' > /volume/test.txt"
   - Location: /volume/test.txt (inside container)
   - Creates file via volume mount

6. Verify on host:
   - Check file: C:\Temp\a1b2c3d4\test.txt
   - Exists: ✅ TRUE
   - Content: ",,,"
   - Matches: ✅ TRUE
```

**Cleanup Phase:**
```
7. Stop container
   - Container receives SIGTERM signal
   - Gracefully stops (timeout 30s)

8. Delete container
   - Container removed from system
   - No trace left

9. Cleanup temp directory
   - Delete C:\Temp\a1b2c3d4\ and contents
   - Best-effort cleanup (ignore errors)
```

#### Expected Output (Verbose):
```
[INFO] Creating container with image alpine:latest, name test-exec-e5f6g7h8
[INFO] Creating container with image alpine:latest
[INFO] Starting container e5f6g7h8c9d0e1f2g3h4i5j6k7l8m9n0
[INFO] Executing commands in container e5f6g7h8c9d0e1f2g3h4i5j6k7l8m9n0
[PASS] File found at C:\Temp\a1b2c3d4\test.txt
[PASS] File content verified: ",,," = ",,,"
[INFO] Stopping container e5f6g7h8c9d0e1f2g3h4i5j6k7l8m9n0
[INFO] Deleting container e5f6g7h8c9d0e1f2g3h4i5j6k7l8m9n0
[xUnit.net 00:00:07.89] RedMaple.Orchestration.Containers.IntegrationTests.CreateTests.CreateContainer_WithVolumeMount_ExecAndVerifyFile(backend: Docker) [PASS]
```

---

### Test 3: CreateContainer_WithPortMapping_IsSuccessful

#### Execution Flow:

**Setup:**
```
1. Image: registry:2 (auto-pulled if needed)
2. Container name: test-registry-a1b2c3d4
3. Port mapping: 5555 (host) → 5000 (container)
```

**Port Binding:**
```
4. Create container with:
   - ExposedPorts: { "5000/tcp": {} }
   - PortBindings: { "5000/tcp": [{ HostPort: "5555" }] }

5. Docker translates to:
   - iptables rule: localhost:5555 → 127.0.0.1:5000
   - Registry service listening on :5000
   - Accessible via http://localhost:5555/
```

**Verification:**
```
6. Start container
7. Wait 2000ms for registry initialization
8. Create HttpClient with 5s timeout
9. GET http://localhost:5555/v2/
   - Response: 200 OK (v2 available)
   - Response: 401 Unauthorized (requires auth)
10. Assert: statusCode == OK || statusCode == Unauthorized ✅
```

**Cleanup:**
```
11. Stop container
12. Delete container
13. Port 5555 released
```

#### Expected Output:
```
[INFO] Creating container with image registry:2
[INFO] Starting container a1b2c3d4e5f6g7h8
[PASS] HTTP GET http://localhost:5555/v2/
[PASS] Status Code: 401 Unauthorized (authentication required)
[INFO] Stopping container a1b2c3d4e5f6g7h8
[INFO] Deleting container a1b2c3d4e5f6g7h8
[xUnit.net 00:00:04.12] RedMaple.Orchestration.Containers.IntegrationTests.CreateTests.CreateContainer_WithPortMapping_IsSuccessful(backend: Docker) [PASS]
```

---

## Full Test Run Output Example

```
========== Starting test discovery ==========
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4
[xUnit.net 00:00:00.07]   Discovering: RedMaple.Orchestration.Containers.IntegrationTests
[xUnit.net 00:00:00.12]   Discovered: 21 tests
========== Test discovery finished: 21 Tests found in 464 ms ==========

========== Starting test run ==========
[xUnit.net 00:00:00.50] Starting test execution, please wait...

[xUnit.net 00:00:05.23] ✅ CreateContainer_IsSuccessful(backend: Docker) [PASS]
[xUnit.net 00:00:05.45] ✅ CreateContainer_IsSuccessful(backend: Podman) [PASS]
[xUnit.net 00:00:05.67] ✅ CreateContainer_IsSuccessful(backend: WslContainers) [PASS]

[xUnit.net 00:00:11.89] ✅ CreateAndDeleteContainer_ContainerIsRemovedFromListing(backend: Docker) [PASS]
[xUnit.net 00:00:12.11] ✅ CreateAndDeleteContainer_ContainerIsRemovedFromListing(backend: Podman) [PASS]
[xUnit.net 00:00:12.33] ✅ CreateAndDeleteContainer_ContainerIsRemovedFromListing(backend: WslContainers) [PASS]

[xUnit.net 00:00:18.55] ✅ CreateContainer_WithVolumeMount_IsSuccessful(backend: Docker) [PASS]
[xUnit.net 00:00:18.77] ✅ CreateContainer_WithVolumeMount_IsSuccessful(backend: Podman) [PASS]
[xUnit.net 00:00:18.99] ✅ CreateContainer_WithVolumeMount_IsSuccessful(backend: WslContainers) [PASS]

[xUnit.net 00:00:26.21] ✅ CreateContainer_WithVolumeMount_ExecAndVerifyFile(backend: Docker) [PASS]
[xUnit.net 00:00:26.43] ✅ CreateContainer_WithVolumeMount_ExecAndVerifyFile(backend: Podman) [PASS]
[xUnit.net 00:00:26.65] ✅ CreateContainer_WithVolumeMount_ExecAndVerifyFile(backend: WslContainers) [PASS]

[xUnit.net 00:00:31.87] ✅ CreateContainer_WithPortMapping_IsSuccessful(backend: Docker) [PASS]
[xUnit.net 00:00:32.09] ✅ CreateContainer_WithPortMapping_IsSuccessful(backend: Podman) [PASS]
[xUnit.net 00:00:32.31] ✅ CreateContainer_WithPortMapping_IsSuccessful(backend: WslContainers) [PASS]

========== Test run finished ==========
✅ Test run completed successfully
📊 Results: 15 Passed, 0 Failed, 0 Skipped
⏱️  Duration: 32.31 seconds
```

---

## Failure Example & Recovery

### Scenario: Image Not Available (First Run)

**Output:**
```
[xUnit.net 00:00:02.15] CreateContainer_IsSuccessful(backend: Docker) [FAIL]
[Error] Docker API responded with status code=NotFound, response={"message":"No such image: alpine:latest"}
```

**Automatic Recovery:**
```
1. HasImageAsync() returns false
2. GetAlpineImageAsync() calls PullImageAsync()
3. [xUnit.net 00:00:15.40] Pulling image alpine:latest
4. [xUnit.net 00:00:32.05] Pull complete
5. Test restarts and PASSES
```

**Final Result:**
```
[xUnit.net 00:00:35.21] CreateContainer_IsSuccessful(backend: Docker) [PASS]
```

---

### Scenario: Port Already in Use

**Output (Port 5555 occupied):**
```
[xUnit.net 00:00:04.12] CreateContainer_WithPortMapping_IsSuccessful(backend: Docker) [FAIL]
[Error] System.Threading.Tasks.TaskCanceledException: The request was canceled due to the configured HttpClient.Timeout of 5 seconds elapsing.
```

**Resolution:**
```
1. Check what's using port 5555:
   netstat -ano | findstr :5555

2. Stop the process or use different port

3. Re-run test:
   dotnet test --filter "CreateContainer_WithPortMapping_IsSuccessful"

4. Result: [PASS] if port is free
```

---

## Debug Output Example

### With Extended Logging:

```
[INFO] Creating fixture for backend: Docker
[INFO] Building service provider
[INFO] Creating LocalContainersClient
[INFO] Getting containers by name: test-create-a1b2c3d4
[INFO] Creating container with image alpine:latest, name test-create-a1b2c3d4
[DEBUG] Container name: test-create-a1b2c3d4
[DEBUG] Container image: alpine:latest
[DEBUG] Container cmd: ["sleep", "1000"]
[INFO] Creating container...
[DEBUG] Container ID: a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
[DEBUG] Querying containers...
[DEBUG] Found 3 containers total
[DEBUG] Checking container: a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
[DEBUG] Container name matches: test-create-a1b2c3d4
[INFO] Container created and verified
[INFO] Cleanup: Deleting container a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
[DEBUG] Container deleted
[INFO] Test completed: PASSED
```

---

## Performance Metrics

### Cold Run (Images Not Cached):
```
Total Time: ~90 seconds

Breakdown:
- Image pulls (2 images):        ~40 seconds
- Container operations (15):     ~35 seconds
- Network operations:            ~10 seconds
- Overhead:                      ~5 seconds
```

### Warm Run (Images Cached):
```
Total Time: ~30 seconds

Breakdown:
- Container operations (15):     ~25 seconds
- Network operations:            ~3 seconds
- Overhead:                      ~2 seconds
```

### Per-Test Breakdown:
```
CreateContainer_IsSuccessful           ~2.2 seconds
CreateAndDeleteContainer_...           ~2.3 seconds
CreateContainer_WithVolumeMount_...    ~2.1 seconds
CreateContainer_WithVolumeMount_Exec   ~2.4 seconds
CreateContainer_WithPortMapping_...    ~2.8 seconds

Average: ~2.4 seconds per test
```

---

## Troubleshooting Guide

| Error | Cause | Solution |
|-------|-------|----------|
| Docker API responded with status code=NotFound | Image missing | Wait for auto-pull or manually: `docker pull alpine:latest` |
| Failed to create container: Invalid volume... | WslContainers old version | Update to WslcClient 2.0+ |
| The request was canceled due to timeout | Port mapping issue | Check port 5555 not in use; increase delay |
| No such image: alpine:latest | Image cache miss | Tests auto-retry; run again |
| Access denied | Docker daemon not running | Start Docker Desktop or Docker service |
| Container already exists | Previous test didn't cleanup | Delete manually: `docker rm -f test-*` |

---

## Next Steps

1. **Run Full Suite:**
   ```
   dotnet test src\RedMaple.Orchestration.Containers.IntegrationTests
   ```

2. **Run Specific Backend:**
   ```
   dotnet test --filter "CreateTests AND (Docker)"
   ```

3. **Add to CI/CD:**
   ```yaml
   - name: Run Integration Tests
	 run: dotnet test --filter CreateTests
   ```

4. **Monitor Results:**
   - Check Test Explorer regularly
   - Watch for cleanup failures (orphaned containers)
   - Monitor Docker daemon resource usage

5. **Extend Tests:**
   - Copy existing test pattern
   - Modify container configuration
   - Add new assertions
   - Ensure cleanup in finally block
