using System.Net;
using System.Net.Sockets;

using Docker.DotNet.Models;
using GuerrillaNtp;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    public class PortBindingTests
    {
        /// <summary>
        /// Global lock for port detection to prevent race conditions in parallel tests
        /// </summary>
        private static readonly SemaphoreSlim _portDetectionLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// An IP address on the local host that is used to test binding to a specific interface
        /// </summary>
        private const string NTP_SERVER_1 = "192.168.0.59";

        /// <summary>
        /// An IP address on the local host that is used to test binding to a specific interface
        /// </summary>
        private const string NTP_SERVER_2 = "192.168.0.241";

        private async Task<string> GetRegistryImageAsync(ILocalContainersClient client)
        {
            const string image = "registry:2";
            try
            {
                if (!await client.HasImageAsync(image, default))
                {
                    await client.PullImageAsync(image, IProgress<JSONMessage>.NullProgress, null, default);
                }
            }
            catch
            {
                // Fallback: try the image anyway, might already be there
            }
            return image;
        }

        /// <summary>
        /// Used for NTP testing, binding to UDP port 123
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task<string> GetNtpServerImageAsync(ILocalContainersClient client)
        {
            const string image = "jerkerolofsson/testbucket-ntp:latest";
            try
            {
                if (!await client.HasImageAsync(image, default))
                {
                    await client.PullImageAsync(image, IProgress<JSONMessage>.NullProgress, null, default);
                }
            }
            catch
            {
                // Fallback: try the image anyway, might already be there
            }
            return image;
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithPortMapping_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetRegistryImageAsync(client);

            var containerName = $"test-registry-{Guid.NewGuid().ToString()[..8]}";
            const int containerPort = 5000;
            var hostPort = DetectAvailablePort("127.0.0.1", Random.Shared.Next(10000,65535));

            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{containerPort}/tcp", default }
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            $"{containerPort}/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostIP = "127.0.0.1",
                                    HostPort = hostPort.ToString()
                                }
                            }
                        }
                    }
                }
            }, default);

            var containerId = response?.ID;
            Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

            try
            {
                // Start the container
                await client.StartAsync(containerId, default);

                // Wait for container to be ready with exponential backoff
                var timeout = TimeSpan.FromSeconds(300);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool connected = false;

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);

                while (stopwatch.Elapsed < timeout && !connected)
                {
                    try
                    {
                        var httpResponse = await httpClient.GetAsync($"http://127.0.0.1:{hostPort}/v2/");

                        // Registry should return 200 or 401 for v2 endpoint
                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                            httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            connected = true;
                        }
                        else
                        {
                            Assert.Fail($"Expected OK or Unauthorized, got {httpResponse.StatusCode}");
                        }
                    }
                    catch (TaskCanceledException) when (stopwatch.Elapsed < timeout)
                    {
                        // Timeout, container not ready yet, wait and retry
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                    catch (HttpRequestException) when (stopwatch.Elapsed < timeout)
                    {
                        // Container not ready yet, wait and retry
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                }

                Assert.True(connected, $"Failed to connect to registry on port {hostPort} within {timeout.TotalSeconds} seconds");
            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.StopAsync(containerId, default);
                    }
                }
                catch { }
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }

         private int DetectAvailablePort(string host, int port, string protocol = "tcp")
        {
            // Use a lock to prevent race conditions when multiple tests run in parallel
            // This ensures only one thread detects and allocates a port at a time
            _portDetectionLock.Wait();
            try
            {
                // Detect an available port while holding the lock
                int currentPort = port;
                int initialPort = port;

                while (true)
                {
                    if (currentPort == initialPort - 1)
                    {
                        throw new InvalidOperationException("Failed to find an available port");
                    }

                    try
                    {
                        if (protocol.Equals("udp", StringComparison.OrdinalIgnoreCase))
                        {
                            // For UDP, try to bind to check availability
                            // Close immediately after check - the container will bind when created
                            var udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(host), currentPort));
                            udpClient.Dispose();
                            return currentPort;
                        }
                        else
                        {
                            // For TCP, try to bind to check availability
                            // Close immediately after check - the container will bind when created
                            var tcpListener = new TcpListener(IPAddress.Parse(host), currentPort);
                            tcpListener.Start();
                            tcpListener.Stop();
                            return currentPort;
                        }
                    }
                    catch
                    {
                        // Port is in use, try next
                        currentPort++;
                    }
                }
            }
            finally
            {
                _portDetectionLock.Release();
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        //[InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithUdpPortMapping_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetNtpServerImageAsync(client);

            var containerName = $"test-ntp-{Guid.NewGuid().ToString()[..8]}";
            const int containerPort = 60123;
            var hostPort = DetectAvailablePort(NTP_SERVER_1, Random.Shared.Next(10000, 65535), "udp");

            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{containerPort}/udp", default }
                },
                Env = new List<string>
                {
                    "NTP_UPSTREAM_SERVER=pool.ntp.org"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            $"{containerPort}/udp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostIP = NTP_SERVER_1,
                                    HostPort = hostPort.ToString()
                                }
                            }
                        }
                    }
                }
            }, default);

            var containerId = response?.ID;
            Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

            try
            {
                // Start the container
                await client.StartAsync(containerId, default);

                // Give container time to start
                await Task.Delay(backend == ContainerBackend.Podman ? 10000 : 5000);

                // Try to connect to the NTP server via UDP on the specified IP
                using CancellationTokenSource ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var ntpClient = new NtpClient(NTP_SERVER_1, TimeSpan.FromSeconds(5), port:hostPort);
                var ntpClock = await ntpClient.QueryAsync(ctsTimeout.Token);

                // If we got a response, port mapping is working
                Assert.NotNull(ntpClock);
                Assert.NotEqual(DateTime.MinValue, ntpClock.UtcNow.UtcDateTime);
            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.StopAsync(containerId, default);
                    }
                }
                catch { }
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithUdpPortMapping_FailsOnWrongInterface(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetNtpServerImageAsync(client);

            var containerName = $"test-ntp-wrong-interface-{Guid.NewGuid().ToString()[..8]}";
            const int containerPort = 60125;
            var hostPort = DetectAvailablePort(NTP_SERVER_1, Random.Shared.Next(10000, 65535), "udp");

            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{containerPort}/udp", default }
                },
                Env = new List<string>
                {
                    "NTP_UPSTREAM_SERVER=pool.ntp.org"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            $"{containerPort}/udp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostIP = NTP_SERVER_1,
                                    HostPort = hostPort.ToString()
                                }
                            }
                        }
                    }
                }
            }, default);

            var containerId = response?.ID;
            Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

            try
            {
                // Start the container
                await client.StartAsync(containerId, default);

                // Give container time to start
                await Task.Delay(backend == ContainerBackend.Podman ? 10000 : 5000);

                // Try to connect to the NTP server via UDP on the WRONG IP address (NTP_SERVER_2 instead of NTP_SERVER_1)
                // This should fail because the container is bound to NTP_SERVER_1, not NTP_SERVER_2
                using CancellationTokenSource ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var ntpClient = new NtpClient(NTP_SERVER_2, TimeSpan.FromSeconds(5), port: hostPort);

                // We expect this to throw an exception because the NTP server is not listening on LOCALHOST_IP
                var ex = await Assert.ThrowsAnyAsync<Exception>(() => ntpClient.QueryAsync(ctsTimeout.Token));
                Assert.NotNull(ex);
            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.StopAsync(containerId, default);
                    }
                }
                catch { }
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }
    }
}
