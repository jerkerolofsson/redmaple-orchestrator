using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class GlobalDnsRepository : DocumentStore<DnsEntry>, IGlobalDnsRepository
    {
        protected override string SaveFilePath => GetConfigFilePath();

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
            await CommitAsync(entries);
        }

        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            return await LoadAsync();
        }


    }
}
