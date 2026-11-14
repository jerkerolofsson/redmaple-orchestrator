using System;
using System.Collections.Generic;
using System.Text;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    internal class TempDirectory : IDisposable
    {
        private readonly string _path;

        public string Path => _path;

        public TempDirectory()
        {
            _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_path, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
