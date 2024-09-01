using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using RedMaple.Orchestrator.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Dns
{
    internal class DnsMasq : IDns
    {
        private readonly ILocalContainersClient mLocalContainersClient;

        public DnsMasq(ILocalContainersClient localContainersClient)
        {
            mLocalContainersClient = localContainersClient;
        }

        public Task AddEntryAsync(string hostname, string ipAddress)
        {
            var hostsFile = GetLocalConfigPath();
            var lines = File.ReadAllLines(hostsFile);

            var line = $"{ipAddress} {hostname}";
            if(!lines.Contains(line))
            {
                lines = [.. lines, line];
                File.WriteAllLines(hostsFile, lines);
            }
            return Task.CompletedTask;
        }

        public Task RemoveEntryAsync(string hostname)
        {
            var hostsFile = GetLocalConfigPath();
            var lines = File.ReadAllLines(hostsFile);
            File.WriteAllLines(hostsFile, lines.Where(x => !x.Contains(hostname)).ToArray());
            return Task.CompletedTask;
        }

        private string GetLocalConfigDir()
        {
            string path = "/var/redmaple/dnsmasq";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\dnsmasq";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        private string GetLocalConfigPath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "hosts");

            if(!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            return filePath;
        }

        public async Task StartAsync()
        {
            var name = "redmaple-dns";
            bool result = await mLocalContainersClient.HasContainerByNameAsync(name);
            if (!result)
            {
                var configFile = GetLocalConfigPath();

                await mLocalContainersClient.CreateContainerAsync(new CreateContainerParameters
                {
                    Name = "redmaple-dns",
                    Image = "strm/dnsmasq",
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "53/udp", new EmptyStruct() }
                    },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string> { $"{configFile}:/etc/hosts" },
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "53/udp", new List<PortBinding>() { new PortBinding() { HostPort = "53" } } }
                        },
                        CapAdd = new List<string> { "NET_ADMIN" }
                    }
                });
            }

            var container = await mLocalContainersClient.GetContainerByNameAsync(name);
            if(container is null)
            {
                throw new Exception("Failed to create container");
            }
            if(!container.IsRunning)
            {
                await mLocalContainersClient.StartAsync(container.Id);
            }
        }

        public async Task StopAsync()
        {
            var name = "redmaple-dns";
            var container = await mLocalContainersClient.GetContainerByNameAsync(name);
            if (container is not null)
            {
                if (container.IsRunning)
                {
                    await mLocalContainersClient.StopAsync(container.Id);
                }
            }
        }
    }
}
