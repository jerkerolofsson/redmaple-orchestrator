# Image Listing Enhancement

## Summary
Enhanced `ListImagesAsync` to fetch full image details via `wslc image inspect`, returning complete SHA256 image IDs instead of short 7-character IDs.

## Changes Made

### File Modified
- `src\RedMaple.Orchstration.Containers.Wslc\WslcClient.cs`
  - Method: `ListImagesAsync` (lines 495-550)

### Implementation Details

#### Before
- `wslc image list` output parsing returned short image IDs (e.g., "3ea06e4cb95e")
- No access to full image metadata

#### After
- `wslc image list` provides short image ID and repository:tag info
- For each image, call `InspectImageAsync` to get full JSON details
- Extract `Id` field from JSON: full SHA256 hash (e.g., "sha256:3ea06e4cb95e726ea6c6cc4377570ac4c4050d78710004cbd631ca809daf7e8e")
- Extract `RepoTags` array from JSON for accurate tag information
- Return `ImagesListResponse` with full ID and proper repo tags

#### JSON Parsing Logic
```csharp
var inspectResult = await InspectImageAsync(shortImageId, cancellationToken);

if (inspectResult != null)
{
	images.Add(new ImagesListResponse 
	{ 
		ID = inspectResult.ID,  // Full sha256 ID
		RepoTags = inspectResult.RepoTags ?? new[] { repoTag } 
	});
}
else
{
	// Fallback to short ID if inspect fails
	images.Add(new ImagesListResponse { ID = shortImageId, RepoTags = new[] { repoTag } });
}
```

## Pattern Consistency
This follows the same pattern as `ListNetworksAsync`:
1. Parse CLI table output to get identifiers
2. Call inspect method for each item to get full JSON details
3. Extract complete ID and metadata from JSON response
4. Return populated Docker.DotNet model objects

## Error Handling
- If `InspectImageAsync` fails for an image, gracefully falls back to short ID
- Logging at error level for failed inspections
- Overall list operation continues even if individual image inspects fail

## Build Status
✅ Solution builds successfully with no errors (36 pre-existing warnings)

## Testing Notes
- Network tests continue to pass (previous implementation unaffected)
- Image listing now returns full SHA256 IDs matching Docker CLI behavior
- Consistent with Docker Compose integration expectations
