﻿using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedMaple.Orchestrator.Containers;
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
        private const string CONTAINER_NAME = "redmaple-dns";
        private readonly ILogger<DnsMasq> _logger;
        private readonly ILocalContainersClient _localContainersClient;
        private readonly SemaphoreSlim _startStopLock = new SemaphoreSlim(1);

        public DnsMasq(
            ILogger<DnsMasq> logger,
            ILocalContainersClient localContainersClient)
        {
            _logger = logger;
            _localContainersClient = localContainersClient;
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
                path = @"C:\temp\redmaple\node\dns";
            }
            if (!Directory.Exists(path))
            {
                _logger.LogInformation("Creating dnsmasq configuration root dir: {path}", path);
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
                _logger.LogDebug("Creating dnsmasq.d configuration dir: {path} exists", path);
                Directory.CreateDirectory(configD);
            }
            return configD;
        }

        private string CreateManagedConfIfItDoesntExist() => GetManagedConfigFilePath();

        /// <summary>
        /// Returns the path to the file where managed (through API) addresses
        /// are located
        /// </summary>
        /// <returns></returns>
        private async Task<string> CreateDnsmasqConfIfItDoesntExistAsync()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "dnsmasq.conf");
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Creating default {path}", filePath);
                await SetUpstreamDnsServersAsync("8.8.8.8", "1.1.1.1");
            }
            return filePath;
        }

        /// <summary>
        /// Sets upstream DNS servers
        /// </summary>
        /// <param name="dns1"></param>
        /// <param name="dns2"></param>
        /// <returns></returns>
        public async Task SetUpstreamDnsServersAsync(string? dns1,string? dns2)
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "dnsmasq.conf");
            _logger.LogDebug("Creating default {path}", filePath);

            dns1 ??= "8.8.8.8";
            dns2 ??= "8.8.4.4";

            using var writer = File.CreateText(filePath);
            await writer.WriteAsync($"""
                no-resolv

                # Upstream DNS servers
                server={dns1}
                server={dns2}

                conf-file=/etc/dnsmasq.d/global.conf
                conf-file=/etc/dnsmasq.d/managed.conf
                    
                """);
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
            await _startStopLock.WaitAsync();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var name = CONTAINER_NAME;
                bool result = await _localContainersClient.HasContainerByNameAsync(name, cts.Token);
                if (!result)
                {
                    string dnsmasqD = GetLocalDnsmasqDDir();
                    var managedConf = CreateManagedConfIfItDoesntExist();
                    var dnsmasqDotConf = await CreateDnsmasqConfIfItDoesntExistAsync();

                    _logger.LogInformation("Creating DNS container, dnsmasq.conf={DnsMasqConf}, dnsmasq.d={DnsMasqD}, managed.conf={ManagedConf}", dnsmasqDotConf, dnsmasqD, managedConf);

                    await CreateContainerAsync(dnsmasqD, dnsmasqDotConf);
                }

                var container = await _localContainersClient.GetContainerByNameAsync(name, cts.Token);
                if (container is null)
                {
                    throw new Exception("Failed to create container");
                }

                _logger.LogInformation("StartAsync: dnsmasq container id={id}, state={state}, status={status}", container.Id, container.State, container.Status);
                if (!container.IsRunning)
                {
                    _logger.LogInformation("StartAsync: Starting dnsmasq container..");
                    using var ctsStart = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                    await _localContainersClient.StartAsync(container.Id, ctsStart.Token);
                }
                else
                {
                    _logger.LogInformation("StartAsync: dnsmasq container is already running, state={state}, status={status} ..", container.State, container.Status);
                }
            }
            finally
            {
                _startStopLock.Release();
            }
        }


        public async Task StopAsync()
        {
            await _startStopLock.WaitAsync();
            try
            {
                var name = CONTAINER_NAME;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var container = await _localContainersClient.GetContainerByNameAsync(name, cts.Token);
                if (container is not null)
                {
                    _logger.LogInformation("StopAsync: dnsmasq container id={id}, state={state}, status={status}", container.Id, container.State, container.Status);
                    if (container.IsRunning)
                    {
                        _logger.LogInformation("StopAsync: Stopping dnsmasq container..");
                        await _localContainersClient.StopAsync(container.Id, cts.Token);
                    }
                    else
                    {
                        _logger.LogInformation("StopAsync: dnsmasq is not running");

                    }
                }
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        private async Task CreateContainerAsync(string dnsmasqD, string dnsmasqDotConf)
        {
            using var ctsCreate = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            await _localContainersClient.CreateContainerAsync(new CreateContainerParameters
            {
                Name = CONTAINER_NAME,
                Image = "strm/dnsmasq",
                ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "53/udp", new EmptyStruct() },
                        { "53/tcp", new EmptyStruct() },
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
                            { "53/udp", new List<PortBinding>() { new PortBinding() { HostPort = "53" } } },
                            { "53/tcp", new List<PortBinding>() { new PortBinding() { HostPort = "53" } } }
                        },
                    CapAdd = new List<string> { "NET_ADMIN" }
                }
            }, ctsCreate.Token);
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
                _logger.LogInformation("Local DNS entry: {dnsmasqLine}", line);
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
                _logger.LogInformation("Global DNS entry: {dnsmasqLine}", line);
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

        public async Task ReloadConfigurationAsync(CancellationToken cancellationToken)
        {
            var container = await _localContainersClient.GetContainerByNameAsync(CONTAINER_NAME);

            if (container is null)
            {
                await StartAsync();
                container = await _localContainersClient.GetContainerByNameAsync(CONTAINER_NAME);
                if (container is null)
                {
                    throw new Exception();
                }
            }

            if (!container.IsRunning)
            {
                _logger.LogInformation("StartAsync: Starting dnsmasq");
                using var ctsStart = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _localContainersClient.StartAsync(container.Id, ctsStart.Token);
            }
            else
            {
                _logger.LogInformation("StartAsync: dnsmasq is already running, reloading config");

                //List<string> command = ["killall", "-HUP", "dnsmasq"];
                //await _localContainersClient.ExecAsync(container.Id, command, cancellationToken);

                await _localContainersClient.StopAsync(container.Id, cancellationToken);
                await _localContainersClient.StartAsync(container.Id, cancellationToken);
            }
        }
    }
}
