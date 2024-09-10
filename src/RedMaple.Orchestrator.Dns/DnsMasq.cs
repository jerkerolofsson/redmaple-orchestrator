using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DnsMasq> _logger;
        private readonly ILocalContainersClient mLocalContainersClient;

        public DnsMasq(
            ILogger<DnsMasq> logger,
            ILocalContainersClient localContainersClient)
        {
            _logger = logger;
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
            string path = "/data/redmaple/node/dns";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\dnsmasq";
            }
            if (!Directory.Exists(path))
            {
                _logger.LogInformation("Creating dnsmasq configuration root dir: {path}", path);
                Directory.CreateDirectory(path);
            }
            else
            {
                _logger.LogDebug("dnsmasq configuration root dir: {path} exists", path);
            }
            return path;
        }

        private string GetLocalDnsmasqDDir()
        {
            string path = GetLocalConfigDir();

            var configD = Path.Combine(path, "dnsmasq.d");
            if (!Directory.Exists(configD))
            {
                _logger.LogDebug("Creating dnsmasq.d configuration dir: {path} exists", path);
                Directory.CreateDirectory(configD);
            }
            else 
            {
                _logger.LogDebug("dnsmasq.d configuration dir: {path} exists", path);
            }
            return configD;
        }

        private string CreateManagedConfIfItDoesntExist() => GetManagedConfigFilePath();

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
                _logger.LogDebug("Creating default {path}", filePath);

                using var writer = File.CreateText(filePath);
                writer.WriteLine("""
                    no-resolv

                    #use google as default nameservers
                    server=8.8.4.4
                    server=8.8.8.8

                    conf-file=/etc/dnsmasq.d/global.conf
                    conf-file=/etc/dnsmasq.d/managed.conf
                    
                    """);

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
                _logger.LogDebug("Creating default {path}", filePath);

                using var writer = File.CreateText(filePath);
                writer.WriteLine("""
                    # Default
                    """);
            }
            return filePath;
        }


        /// <summary>
        /// Returns the path to the file where globally managed (through API) addresses
        /// are located
        /// </summary>
        /// <returns></returns>
        private string GetGlobalConfigFilePath()
        {
            string dir = GetLocalDnsmasqDDir();
            var filePath = Path.Combine(dir, "global.conf");

            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Creating default {path}", filePath);

                using var writer = File.CreateText(filePath);
                writer.WriteLine("""
                    # Default
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
                var managedConf = CreateManagedConfIfItDoesntExist();
                var dnsmasqDotConf = CreateDnsmasqConfIfItDoesntExist();

                _logger.LogInformation("Creating DNS container, dnsmasq.conf={DnsMasqConf}, dnsmasq.d={DnsMasqD}, managed.conf={ManagedConf}", dnsmasqDotConf, dnsmasqD, managedConf);

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
                            $"{dnsmasqDotConf}:/etc/dnsmasq.conf:z",
                            $"{dnsmasqD}:/etc/dnsmasq.d:z" 
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
            await SetManagedDnsEntriesAsync(entries.Where(x => !x.IsGlobal));
            await SetGlobalDnsEntriesAsync(entries.Where(x => x.IsGlobal));
        }

        private async Task SetManagedDnsEntriesAsync(IEnumerable<DnsEntry> entries)
        {
            var managedDnsMasqConfFile = GetManagedConfigFilePath();
            using var writer = File.CreateText(managedDnsMasqConfFile);
            foreach (var entry in entries)
            {
                var line = $"address=/{entry.Hostname}/{entry.IpAddress}";
                await writer.WriteLineAsync(line);
            }
        }

        private async Task SetGlobalDnsEntriesAsync(IEnumerable<DnsEntry> entries)
        {
            var managedDnsMasqConfFile = GetGlobalConfigFilePath();
            using var writer = File.CreateText(managedDnsMasqConfFile);
            foreach (var entry in entries)
            {
                var line = $"address=/{entry.Hostname}/{entry.IpAddress}";
                await writer.WriteLineAsync(line);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            var entries = new List<DnsEntry>();

            await GetManagedDnsEntriesAsync(entries);
            await GetGlobalDnsEntriesAsync(entries);

            return entries;
        }

        private async Task GetGlobalDnsEntriesAsync(List<DnsEntry> entries)
        {
            var managedDnsMasqConfFile = GetGlobalConfigFilePath();
            var lines = await File.ReadAllLinesAsync(managedDnsMasqConfFile);

            foreach (var line in lines)
            {
                if (line.StartsWith("address="))
                {
                    var items = line.Split('/');
                    if (items.Length == 3)
                    {
                        entries.Add(new DnsEntry { Hostname = items[1], IpAddress = items[2], IsGlobal = true });
                    }
                }
            }
        }
        private async Task GetManagedDnsEntriesAsync(List<DnsEntry> entries)
        {
            var managedDnsMasqConfFile = GetManagedConfigFilePath();
            var lines = await File.ReadAllLinesAsync(managedDnsMasqConfFile);

            foreach (var line in lines)
            {
                if (line.StartsWith("address="))
                {
                    var items = line.Split('/');
                    if (items.Length == 3)
                    {
                        entries.Add(new DnsEntry { Hostname = items[1], IpAddress = items[2], IsGlobal = false });
                    }
                }
            }
        }
    }
}
