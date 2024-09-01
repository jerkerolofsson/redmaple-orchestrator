using RedMaple.Orchestrator.Security.Models;

namespace CertificateTests
{
    public class IntemediateCaTests
    {
        [Fact]
        public async Task GetCaCertificateAsync_NoExceptions()
        {
            using var framework = new CertificateTestFramework();
            await framework.CA.GetCaCertificateAsync();
        }

        [Fact]
        public async Task FindCertificateByNameAsync_AfterGenerateCertificate()
        {
            using var framework = new CertificateTestFramework();
            await framework.CA.GetCaCertificateAsync();

            var root = await framework.Provider.FindCertificateByNameAsync("ca");
            Assert.NotNull(root);
            Assert.NotNull(root.Certificate);
            Assert.NotNull(root.PemKeyPath);
            Assert.NotNull(root.PemCertPath);
            Assert.NotNull(root.CrtPath);
            Assert.NotNull(root.PfxPath);
        }


        [Fact]
        public async Task GenerateCertificateAsync_WithSignByLocalCaTrue()
        {
            var request = new CreateCertificateRequest
            {
                Name = "homelab.lan",
                CommonNames = "homelab.lan",
                SignByLocalCa = true,
                SanDnsNames = "homelab.lan"
            };

            using var framework = new CertificateTestFramework();
            await framework.CA.GenerateCertificateAsync(request);
        }
    }
}