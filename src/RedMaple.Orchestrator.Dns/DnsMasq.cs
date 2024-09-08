using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Dns;
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

        public async Task AddEntryAsync(DnsEntry entry)
        {
            var hostsFile = GetManagedConfigFilePath();
            var lines = await File.ReadAllLinesAsync(hostsFile);

            var line = $"address=/{entry.Hostname}/{entry.IpAddress}";
            if(!lines.Contains(line))
            {
                lines = [.. lines, line];
                File.WriteAllLines(hostsFile, lines);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveEntryAsync(string hostname)
        {
            var hostsFile = GetManagedConfigFilePath();
            var lines = await File.ReadAllLinesAsync(hostsFile);

            var pattern = $"address=/{hostname}";
            File.WriteAllLines(hostsFile, lines.Where(x => !x.Contains(pattern)).ToArray());
        }

        private string GetLocalConfigDir()
        {
            string path = "/data/dnsmasq";
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

        private string GetLocalDnsmasqDDir()
        {
            string path = GetLocalConfigDir();

            var configD = Path.Combine(path, "dnsmasq.d");
            if (!Directory.Exists(configD))
            {
                Directory.CreateDirectory(configD);
            }
            return configD;
        }

        private void CreateManagedConfIfItDoesntExist()
        {
            var _ = GetManagedConfigFilePath();
        }

        /// <summary>
        /// Returns the path to the file where managed (through API) addresses
        /// are located
        /// </summary>
        /// <returns></returns>
        private string CreateDnsmasqConfIfItDoesntExist()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "dnsmasq.conf");
            if (!File.Exists(filePath))
            {
                using var file = File.CreateText(filePath);
                file.WriteLine("conf-file=/etc/dnsmasq.d/managed.conf");
            }
            return filePath;
        }

        /// <summary>
        /// Returns the path to the file where managed (through API) addresses
        /// are located
        /// </summary>
        /// <returns></returns>
        private string GetManagedConfigFilePath()
        {
            string dir = GetLocalDnsmasqDDir();
            var filePath = Path.Combine(dir, "managed.conf");

            if (!File.Exists(filePath))
            {
                using var writer = File.CreateText(filePath);
                writer.WriteLine("""
                    no-resolv

                    #use google as default nameservers
                    server=8.8.4.4
                    server=8.8.8.8

                    conf-file=/etc/dnsmasq.d/managed.conf
                    """);
            }
            return filePath;
        }

        /// <inheritdoc/>
        public async Task StartAsync()
        {
            var name = "redmaple-dns";
            bool result = await mLocalContainersClient.HasContainerByNameAsync(name);
            if (!result)
            {
                string dnsmasqD = GetLocalDnsmasqDDir();
                CreateManagedConfIfItDoesntExist();
                var dnsmasqDotConf = CreateDnsmasqConfIfItDoesntExist();

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
                        Binds = new List<string> 
                        { 
                            $"{dnsmasqDotConf}:/etc/dnsmasq.conf",
                            $"{dnsmasqD}:/etc/dnsmasq.d" 
                        },
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task SetDnsEntriesAsync(List<DnsEntry> entries)
        {
            var managedDnsMasqConfFile = GetManagedConfigFilePath();
            using var writer = File.CreateText(managedDnsMasqConfFile);
            foreach(var entry in entries)
            {
                var line = $"address=/{entry.Hostname}/{entry.IpAddress}";
                await writer.WriteLineAsync(line);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            var entries = new List<DnsEntry>();

            var managedDnsMasqConfFile = GetManagedConfigFilePath();
            var lines = await File.ReadAllLinesAsync(managedDnsMasqConfFile);

            foreach (var line in lines)
            {
                if(line.StartsWith("address="))
                {
                    var items = line.Split('/');
                    if(items.Length == 3)
                    {
                        entries.Add(new DnsEntry { Hostname = items[1], IpAddress = items[2] });
                    }
                }
            }

            return entries;
        }
    }
}
