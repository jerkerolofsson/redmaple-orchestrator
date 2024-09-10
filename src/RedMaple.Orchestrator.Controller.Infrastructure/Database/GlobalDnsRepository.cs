using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Ingress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class GlobalDnsRepository : IGlobalDnsRepository
    {
        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/dns";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private string GetConfigFilePath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "global-dns.json");

            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(new List<DnsEntry>()));
            }
            return filePath;
        }

        public async Task SetDnsEntriesAsync(List<DnsEntry> entries)
        {
            await WriteEntriesAsync(entries);
        }

        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            return await LoadEntriesAsync();
        }

        public async Task<List<DnsEntry>> LoadEntriesAsync()
        {
            var path = GetConfigFilePath();
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var services = JsonSerializer.Deserialize<List<DnsEntry>>(json);
                return services ?? new();
            }
            catch (Exception) { }
            return new List<DnsEntry>();
        }
        private async Task WriteEntriesAsync(List<DnsEntry> services)
        {
            var json = JsonSerializer.Serialize(services);
            var path = GetConfigFilePath();
            await File.WriteAllTextAsync(path, json);
        }

    }
}
