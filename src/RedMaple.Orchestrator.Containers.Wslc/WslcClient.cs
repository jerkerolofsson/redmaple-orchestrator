using System.Diagnostics;
using System.Text.Json;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Containers;

using ContractContainer = RedMaple.Orchestrator.Contracts.Containers.Container;

namespace RedMaple.Orchestrator.Containers.Wslc
{
    /// <summary>
    /// WSL Containers (WSLC) implementation of the local containers client
    /// Uses Microsoft.WSL.Containers SDK for available operations, falls back to CLI for others
    /// </summary>
    public class WslcClient : ILocalContainersClient, IWslcClient
    {
        private readonly ILogger<WslcClient>? _logger;
        private readonly string _wslcExePath;

        public WslcClient(ILogger<WslcClient>? logger = null)
        {
            _logger = logger;
            _wslcExePath = "wslc";
        }

        public void Dispose()
        {
        }

        #region Helper Methods

        /// <summary>
        /// Executes a wslc CLI command and returns the output
        /// </summary>
        private async Task<(int exitCode, string stdout, string stderr)> ExecuteWslcCommandAsync(
            string arguments,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _wslcExePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start wslc process");
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode, stdout, stderr);
        }

        private string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\\'))
            {
                return "\"" + arg.Replace("\"", "\\\"") + "\"";
            }

            return arg;
        }

        #endregion

        #region Container Lifecycle Methods

        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Creating container with image {Image}, name {Name}", parameters.Image, parameters.Name);

            var args = new System.Text.StringBuilder("run -d");

            if (!string.IsNullOrEmpty(parameters.Name))
            {
                args.Append($" --name {EscapeArgument(parameters.Name)}");
            }

            if (parameters.Env?.Count > 0)
            {
                foreach (var env in parameters.Env)
                {
                    args.Append($" -e {EscapeArgument(env)}");
                }
            }

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
                            if (!string.IsNullOrEmpty(binding.HostIP))
                            {
                                // Format: hostIp:hostPort:containerPort/protocol
                                args.Append($" -p {EscapeArgument($"{binding.HostIP}:{binding.HostPort}:{containerPort}")}");

                            }
                            else
                            {
                                // Format: hostPort:containerPort/protocol
                                args.Append($" -p {EscapeArgument($"{binding.HostPort}:{containerPort}")}");
                            }
                        }
                        else
                        {
                            // No host port specified, just expose the container port with protocol
                            args.Append($" -p {EscapeArgument(containerPort)}");
                        }
                    }
                }
            }

            // Handle ExposedPorts without explicit bindings
            if (parameters.ExposedPorts?.Count > 0)
            {
                foreach (var port in parameters.ExposedPorts.Keys)
                {
                    // Only add if not already handled by PortBindings
                    if (parameters.HostConfig?.PortBindings == null || 
                        !parameters.HostConfig.PortBindings.ContainsKey(port))
                    {
                        args.Append($" -p {EscapeArgument(port)}");
                    }
                }
            }

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

            if (parameters.HostConfig?.Privileged == true)
            {
                args.Append(" --privileged");
            }

            args.Append($" {EscapeArgument(parameters.Image)}");

            if (parameters.Cmd?.Count > 0)
            {
                foreach (var cmd in parameters.Cmd)
                {
                    args.Append($" {EscapeArgument(cmd)}");
                }
            }

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args.ToString(), cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to create container: {Error}", stderr);
                throw new InvalidOperationException($"Failed to create container: {stderr}\nCommand line:\n{args.ToString()}");
            }

            var shortId = stdout.Trim();

            // Get the full container ID via inspect
            var fullId = await InspectContainerAsync(shortId, cancellationToken) ?? shortId;

            return new CreateContainerResponse { ID = fullId };
        }

        public async Task<ContractContainer?> GetContainerByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var containers = await GetContainersAsync(null, cancellationToken);
            return containers.FirstOrDefault(c => c.Id == id);
        }

        public async Task<ContractContainer?> GetContainerByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var containers = await GetContainersAsync(null, cancellationToken);
            return containers.FirstOrDefault(c => c.Name == name);
        }

        public async Task<List<ContractContainer>> GetContainersAsync(string? project, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync("list", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogError("Failed to list containers: {Error}", stderr);
                    return new List<ContractContainer>();
                }

                var containers = new List<ContractContainer>();
                var lines = stdout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                    {
                        var shortId = parts[0];

                        // Get the full container details via inspect
                        var (fullId, name, image) = await InspectContainerDetailsAsync(shortId, cancellationToken);

                        containers.Add(new ContractContainer
                        {
                            Id = fullId ?? shortId,
                            Name = name ?? "",
                            Image = image ?? (parts.Length > 1 ? parts[1] : ""),
                            State = parts.Length > 3 ? parts[3] : "unknown",
                            Status = parts.Length > 3 ? parts[3] : "unknown"
                        });
                    }
                }

                return containers;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get containers");
                return new List<ContractContainer>();
            }
        }

        /// <summary>
        /// Inspects a container and returns its full ID and Name by parsing the JSON response from wslc inspect
        /// </summary>
        private async Task<(string? Id, string? Name, string? Image)> InspectContainerDetailsAsync(string containerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync($"inspect {EscapeArgument(containerId)}", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogWarning("Failed to inspect container {ContainerId}: {Error}", containerId, stderr);
                    return (null, null, null);
                }

                // Parse JSON array from wslc inspect response
                using (var doc = JsonDocument.Parse(stdout))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstElement = root[0];

                        string? id = null;
                        if (firstElement.TryGetProperty("Id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                        {
                            id = idElement.GetString();
                        }

                        string? name = null;
                        if (firstElement.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            name = nameElement.GetString();
                            // Remove leading slash if present
                            if (name?.StartsWith("/") == true)
                            {
                                name = name[1..];
                            }
                        }

                        string? image = null;
                        if (firstElement.TryGetProperty("Image", out var imageElement) && imageElement.ValueKind == JsonValueKind.String)
                        {
                            image = imageElement.GetString();
                        }

                        return (id, name, image);
                    }
                }

                _logger?.LogWarning("Failed to parse container details from inspect response for {ContainerId}", containerId);
                return (null, null, null);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Exception while inspecting container {ContainerId}", containerId);
                return (null, null, null);
            }
        }

        /// <summary>
        /// Inspects a container and returns its full ID by parsing the JSON response from wslc inspect
        /// </summary>
        private async Task<string?> InspectContainerAsync(string containerId, CancellationToken cancellationToken = default)
        {
            var (id, _, _) = await InspectContainerDetailsAsync(containerId, cancellationToken);
            return id;
        }

        public async Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var container = await GetContainerByNameAsync(name, cancellationToken);
            return container != null;
        }

        public async Task StartAsync(string id, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Starting container {ContainerId}", id);
            var (exitCode, _, stderr) = await ExecuteWslcCommandAsync($"start {EscapeArgument(id)}", cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to start container: {Error}", stderr);
                throw new InvalidOperationException($"Failed to start container: {stderr}");
            }
        }

        public async Task StopAsync(string id, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Stopping container {ContainerId}", id);
            var (exitCode, _, stderr) = await ExecuteWslcCommandAsync($"stop {EscapeArgument(id)}", cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to stop container: {Error}", stderr);
                throw new InvalidOperationException($"Failed to stop container: {stderr}");
            }
        }

        public async Task RestartAsync(string id, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Restarting container {ContainerId}", id);
            await StopAsync(id, cancellationToken);
            await StartAsync(id, cancellationToken);
        }

        public async Task DeleteContainerAsync(string id, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Deleting container {ContainerId}", id);
            var (exitCode, _, stderr) = await ExecuteWslcCommandAsync($"rm -f {EscapeArgument(id)}", cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to delete container: {Error}", stderr);
                throw new InvalidOperationException($"Failed to delete container: {stderr}");
            }
        }

        public async Task ExecAsync(string containerId, IList<string> commands, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Executing commands in container {ContainerId}", containerId);

            var cmdArgs = string.Join(" ", commands.Select(EscapeArgument));
            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync($"exec {EscapeArgument(containerId)} {cmdArgs}", cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to execute commands: {Error}", stderr);
                throw new InvalidOperationException($"Failed to execute commands: {stderr}");
            }
        }

        #endregion

        #region Image Methods

        public async Task<bool> HasImageAsync(string image, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, _, _) = await ExecuteWslcCommandAsync($"image inspect {EscapeArgument(image)}", cancellationToken);
                return exitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetImageIdAsync(string image, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, _) = await ExecuteWslcCommandAsync($"inspect {EscapeArgument(image)}", cancellationToken);
                if (exitCode == 0)
                {
                    // Parse JSON array from wslc inspect response
                    using (var doc = JsonDocument.Parse(stdout))
                    {
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                        {
                            var firstElement = root[0];
                            if (firstElement.TryGetProperty("Id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                            {
                                return idElement.GetString();
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get image ID");
                return null;
            }
        }

        public async Task<ImageInspectResponse?> InspectImageAsync(string image, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, _) = await ExecuteWslcCommandAsync($"inspect {EscapeArgument(image)}", cancellationToken);
                if (exitCode != 0)
                {
                    return null;
                }

                // Parse JSON array from wslc inspect response
                using (var doc = JsonDocument.Parse(stdout))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstElement = root[0];

                        string? id = null;
                        if (firstElement.TryGetProperty("Id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                        {
                            id = idElement.GetString();
                        }

                        // Try to get RepoTags
                        string[]? repoTags = null;
                        if (firstElement.TryGetProperty("RepoTags", out var repoTagsElement) && repoTagsElement.ValueKind == JsonValueKind.Array)
                        {
                            var tags = new List<string>();
                            foreach (var tag in repoTagsElement.EnumerateArray())
                            {
                                if (tag.ValueKind == JsonValueKind.String)
                                {
                                    var tagStr = tag.GetString();
                                    if (tagStr != null)
                                        tags.Add(tagStr);
                                }
                            }
                            repoTags = tags.ToArray();
                        }

                        if (id != null)
                        {
                            return new ImageInspectResponse
                            {
                                ID = id,
                                RepoTags = repoTags ?? new[] { image }
                            };
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to inspect image");
                return null;
            }
        }

        public async Task<IList<ImagesListResponse>> ListImagesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync("image list", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogError("Failed to list images: {Error}", stderr);
                    return new List<ImagesListResponse>();
                }

                var images = new List<ImagesListResponse>();
                var lines = stdout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Skip header line (REPOSITORY   TAG   IMAGE ID       CREATED       SIZE)
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var repository = parts[0];
                        var tag = parts[1];
                        var shortImageId = parts[2];

                        // Fetch full image details via inspect to get the complete sha256 ID
                        var inspectResult = await InspectImageAsync(shortImageId, cancellationToken);

                        var repoTag = $"{repository}:{tag}";
                        if (inspectResult != null)
                        {
                            images.Add(new ImagesListResponse 
                            { 
                                ID = inspectResult.ID, 
                                RepoTags = inspectResult.RepoTags ?? new[] { repoTag } 
                            });
                        }
                        else
                        {
                            // Fallback to short ID if inspect fails
                            images.Add(new ImagesListResponse { ID = shortImageId, RepoTags = new[] { repoTag } });
                        }
                    }
                }

                return images;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to list images");
                return new List<ImagesListResponse>();
            }
        }

        public async Task PullImageAsync(string imageName, IProgress<JSONMessage> progress, CancellationTokenSource? cts, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Pulling image {ImageName}", imageName);

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync($"image pull {EscapeArgument(imageName)}", cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to pull image: {Error}", stderr);
                throw new InvalidOperationException($"Failed to pull image: {stderr}");
            }

            cts?.Cancel();
        }

        #endregion

        #region Network Methods

        public async Task<NetworksCreateResponse> CreateNetworkAsync(NetworksCreateParameters parameters, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Creating network {NetworkName} with driver {Driver}", parameters.Name, parameters.Driver);

            var args = new System.Text.StringBuilder($"network create");

            if (!string.IsNullOrEmpty(parameters.Driver))
            {
                args.Append($" -d {EscapeArgument(parameters.Driver)}");
            }

            if (parameters.Labels?.Count > 0)
            {
                foreach (var label in parameters.Labels)
                {
                    var labelValue = $"{label.Key}={label.Value}";
                    args.Append($" -l {EscapeArgument(labelValue)}");
                }
            }

            if (parameters.Options?.Count > 0)
            {
                foreach (var opt in parameters.Options)
                {
                    var optValue = $"{opt.Key}={opt.Value}";
                    args.Append($" -o {EscapeArgument(optValue)}");
                }
            }

            if (parameters.Internal == true)
            {
                args.Append(" --internal");
            }

            if (!string.IsNullOrEmpty(parameters.IPAM?.Config?.FirstOrDefault()?.Gateway))
            {
                args.Append($" --gateway {EscapeArgument(parameters.IPAM.Config.First().Gateway)}");
            }

            if (!string.IsNullOrEmpty(parameters.IPAM?.Config?.FirstOrDefault()?.Subnet))
            {
                args.Append($" --subnet {EscapeArgument(parameters.IPAM.Config.First().Subnet)}");
            }

            args.Append($" {EscapeArgument(parameters.Name)}");

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args.ToString(), cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to create network: {Error}", stderr);
                throw new InvalidOperationException($"Failed to create network: {stderr}");
            }

            var networkId = stdout.Trim();
            return new NetworksCreateResponse { ID = networkId };
        }

        public async Task DeleteNetworkAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Deleting network {NetworkId}", id);

            var args = $"network remove {EscapeArgument(id)}";

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args, cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to delete network: {Error}", stderr);
                throw new InvalidOperationException($"Failed to delete network: {stderr}");
            }

            _logger?.LogInformation("Successfully deleted network {NetworkId}", id);
        }

        public async Task<IList<NetworkResponse>> ListNetworksAsync(NetworksListParameters parameters, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync("network list", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogError("Failed to list networks: {Error}", stderr);
                    return new List<NetworkResponse>();
                }

                var networks = new List<NetworkResponse>();
                var lines = stdout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Skip header line (NETWORK ID     NAME      DRIVER    SCOPE)
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var networkName = parts[1];

                        // Fetch full network details via inspect to get complete ID and IPAM
                        var networkDetails = await InspectNetworkAsync(networkName, cancellationToken);
                        if (networkDetails != null)
                        {
                            networks.Add(networkDetails);
                        }
                    }
                }

                return networks;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to list networks");
                return new List<NetworkResponse>();
            }
        }

        /// <summary>
        /// Inspects a network and returns full details including full ID, IPAM, labels, and options
        /// </summary>
        private async Task<NetworkResponse?> InspectNetworkAsync(string networkName, CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync($"network inspect {EscapeArgument(networkName)}", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogWarning("Failed to inspect network {NetworkName}: {Error}", networkName, stderr);
                    return null;
                }

                // Parse JSON array from wslc network inspect response
                using (var doc = JsonDocument.Parse(stdout))
                {
                    var root = doc.RootElement;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstElement = root[0];

                        string? id = null;
                        if (firstElement.TryGetProperty("Id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                        {
                            id = idElement.GetString();
                        }

                        string? name = null;
                        if (firstElement.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                        {
                            name = nameElement.GetString();
                        }

                        string? driver = null;
                        if (firstElement.TryGetProperty("Driver", out var driverElement) && driverElement.ValueKind == JsonValueKind.String)
                        {
                            driver = driverElement.GetString();
                        }

                        string? scope = null;
                        if (firstElement.TryGetProperty("Scope", out var scopeElement) && scopeElement.ValueKind == JsonValueKind.String)
                        {
                            scope = scopeElement.GetString();
                        }

                        // Extract labels
                        var labels = new Dictionary<string, string>();
                        if (firstElement.TryGetProperty("Labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in labelsElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.String)
                                {
                                    labels[prop.Name] = prop.Value.GetString() ?? "";
                                }
                            }
                        }

                        // Extract IPAM configuration
                        IPAM? ipamConfig = null;
                        if (firstElement.TryGetProperty("IPAM", out var ipamElement) && ipamElement.ValueKind == JsonValueKind.Object)
                        {
                            var ipamConfigs = new List<IPAMConfig>();

                            if (ipamElement.TryGetProperty("Config", out var configElement) && configElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var configItem in configElement.EnumerateArray())
                                {
                                    if (configItem.ValueKind == JsonValueKind.Object)
                                    {
                                        string? subnet = null;
                                        if (configItem.TryGetProperty("Subnet", out var subnetElement) && subnetElement.ValueKind == JsonValueKind.String)
                                        {
                                            subnet = subnetElement.GetString();
                                        }

                                        string? gateway = null;
                                        if (configItem.TryGetProperty("Gateway", out var gatewayElement) && gatewayElement.ValueKind == JsonValueKind.String)
                                        {
                                            gateway = gatewayElement.GetString();
                                        }

                                        ipamConfigs.Add(new IPAMConfig
                                        {
                                            Subnet = subnet,
                                            Gateway = gateway
                                        });
                                    }
                                }
                            }

                            string? ipamDriver = null;
                            if (ipamElement.TryGetProperty("Driver", out var driverElement2) && driverElement2.ValueKind == JsonValueKind.String)
                            {
                                ipamDriver = driverElement2.GetString();
                            }

                            var ipamOptions = new Dictionary<string, string>();
                            if (ipamElement.TryGetProperty("Options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in optionsElement.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                    {
                                        ipamOptions[prop.Name] = prop.Value.GetString() ?? "";
                                    }
                                }
                            }

                            ipamConfig = new IPAM
                            {
                                Driver = ipamDriver,
                                Config = ipamConfigs,
                                Options = ipamOptions
                            };
                        }

                        // Extract network options
                        var options = new Dictionary<string, string>();
                        if (firstElement.TryGetProperty("Options", out var netOptionsElement) && netOptionsElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in netOptionsElement.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.String)
                                {
                                    options[prop.Name] = prop.Value.GetString() ?? "";
                                }
                            }
                        }

                        return new NetworkResponse
                        {
                            ID = id ?? name,
                            Name = name,
                            Driver = driver ?? "bridge",
                            Scope = scope ?? "local",
                            Labels = labels,
                            IPAM = ipamConfig,
                            Options = options
                        };
                    }
                }

                _logger?.LogWarning("Failed to parse network details from inspect response for {NetworkName}", networkName);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error inspecting network {NetworkName}", networkName);
                return null;
            }
        }

        public async Task ConnectNetworkAsync(string networkId, NetworkConnectParameters parameters, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Connecting container {ContainerId} to network {NetworkId}", parameters.Container, networkId);

            var args = $"network connect {EscapeArgument(networkId)} {EscapeArgument(parameters.Container)}";

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args, cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to connect container to network: {Error}", stderr);
                throw new InvalidOperationException($"Failed to connect container to network: {stderr}");
            }

            _logger?.LogInformation("Successfully connected container {ContainerId} to network {NetworkId}", parameters.Container, networkId);
        }

        #endregion

        #region Volume Methods

        public async Task<VolumeResponse> CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Creating volume {VolumeName} with driver {Driver}", parameters.Name, parameters.Driver);

            var args = new System.Text.StringBuilder($"volume create");

            if (!string.IsNullOrEmpty(parameters.Driver))
            {
                args.Append($" -d {EscapeArgument(parameters.Driver)}");
            }

            if (parameters.Labels?.Count > 0)
            {
                foreach (var label in parameters.Labels)
                {
                    var labelValue = $"{label.Key}={label.Value}";
                    args.Append($" -l {EscapeArgument(labelValue)}");
                }
            }

            if (parameters.DriverOpts?.Count > 0)
            {
                foreach (var opt in parameters.DriverOpts)
                {
                    var optValue = $"{opt.Key}={opt.Value}";
                    args.Append($" -o {EscapeArgument(optValue)}");
                }
            }

            args.Append($" {EscapeArgument(parameters.Name)}");

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args.ToString(), cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to create volume: {Error}", stderr);
                throw new InvalidOperationException($"Failed to create volume: {stderr}");
            }

            _logger?.LogInformation("Successfully created volume {VolumeName}", parameters.Name);

            return new VolumeResponse
            {
                Name = parameters.Name,
                Driver = parameters.Driver ?? "guest",
                Labels = parameters.Labels,
                Mountpoint = $"/var/lib/docker/volumes/{parameters.Name}/_data"
            };
        }

        public async Task DeleteVolumeAsync(string name, bool force, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Deleting volume {VolumeName}", name);

            var args = new System.Text.StringBuilder($"volume remove");
            if (force)
            {
                args.Append(" -f");
            }
            args.Append($" {EscapeArgument(name)}");

            var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args.ToString(), cancellationToken);

            if (exitCode != 0)
            {
                _logger?.LogError("Failed to delete volume: {Error}", stderr);
                throw new InvalidOperationException($"Failed to delete volume: {stderr}");
            }

            _logger?.LogInformation("Successfully deleted volume {VolumeName}", name);
        }

        public async Task<VolumesListResponse> ListVolumesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync("volume list --format json", cancellationToken);

                if (exitCode != 0)
                {
                    _logger?.LogError("Failed to list volumes: {Error}", stderr);
                    return new VolumesListResponse { Volumes = new List<VolumeResponse>() };
                }

                var volumes = new List<VolumeResponse>();
                var lines = stdout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        using (var doc = JsonDocument.Parse(line))
                        {
                            var root = doc.RootElement;

                            var name = root.TryGetProperty("Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                                ? nameElement.GetString()
                                : null;
                            var driver = root.TryGetProperty("Driver", out var driverElement) && driverElement.ValueKind == JsonValueKind.String
                                ? driverElement.GetString()
                                : "guest";
                            var mountpoint = root.TryGetProperty("Mountpoint", out var mountpointElement) && mountpointElement.ValueKind == JsonValueKind.String
                                ? mountpointElement.GetString()
                                : null;
                            var scope = root.TryGetProperty("Scope", out var scopeElement) && scopeElement.ValueKind == JsonValueKind.String
                                ? scopeElement.GetString()
                                : "local";

                            if (!string.IsNullOrEmpty(name))
                            {
                                var labels = new Dictionary<string, string>();
                                if (root.TryGetProperty("Labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.String)
                                {
                                    var labelsStr = labelsElement.GetString();
                                    if (!string.IsNullOrEmpty(labelsStr))
                                    {
                                        foreach (var labelPair in labelsStr.Split(','))
                                        {
                                            var parts = labelPair.Split('=');
                                            if (parts.Length == 2)
                                            {
                                                labels[parts[0]] = parts[1];
                                            }
                                        }
                                    }
                                }

                                volumes.Add(new VolumeResponse
                                {
                                    Name = name,
                                    Driver = driver,
                                    Mountpoint = mountpoint,
                                    Labels = labels,
                                    Scope = scope
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to parse volume JSON: {Line}", line);
                    }
                }

                return new VolumesListResponse { Volumes = volumes };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to list volumes");
                return new VolumesListResponse { Volumes = new List<VolumeResponse>() };
            }
        }

        public async Task RemoveVolumeAsync(string name, bool force, CancellationToken cancellationToken = default)
        {
            await DeleteVolumeAsync(name, force, cancellationToken);
        }

        #endregion

        #region Log/Stats Methods

        public Task GetLogsAsync(string id, bool follow, string? tail, IProgress<string> callback, CancellationTokenSource cts, CancellationToken cancellationToken = default)
        {
            var _ = Task.Factory.StartNew(async (object? state) =>
            {
                try
                {
                    var args = $"logs {EscapeArgument(id)}";
                    if (follow) args += " -f";
                    if (!string.IsNullOrEmpty(tail)) args += $" --tail {tail}";

                    var (exitCode, stdout, stderr) = await ExecuteWslcCommandAsync(args, cancellationToken);

                    if (exitCode == 0)
                    {
                        foreach (var line in stdout.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                callback.Report(line);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "GetLogsAsync stopped for container {ContainerId}", id);
                }
                finally
                {
                    cts.Cancel();
                }
            }, null, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        public Task GetStatsAsync(string id, IProgress<string> callback, CancellationTokenSource cts, CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("WSL Containers does not support stats collection");
            throw new NotSupportedException("WSL Containers does not support stats collection");
        }

        #endregion

        #region Registry Methods

        public async Task LoginAsync(string registry, string username, string password, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Logging in to registry {Registry} using WSL Containers", registry);

            var psi = new ProcessStartInfo
            {
                FileName = _wslcExePath,
                Arguments = $"login -u {EscapeArgument(username)} -p {EscapeArgument(password)} {EscapeArgument(registry)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start wslc login process");
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger?.LogError("WSL Containers login failed: {Error}", stderr);
                throw new InvalidOperationException($"WSL Containers login failed: {stderr}");
            }

            _logger?.LogInformation("Successfully logged in to registry {Registry} using WSL Containers", registry);
        }

        #endregion
    }
}
