namespace CertificateTests
{
    public class RootCertificateTests
    {
        [Fact]
        public async Task GetRootCertificateAsync_NoExceptions()
        {
            using var framework = new CertificateTestFramework();
            await framework.CA.GetRootCertificateAsync();
        }

        [Fact]
        public async Task FindCertificateByNameAsync_AfterGenerateCertificate()
        {
            using var framework = new CertificateTestFramework();
            await framework.CA.GetRootCertificateAsync();

            var root = await framework.Provider.FindCertificateByNameAsync("root");
            Assert.NotNull(root);
            Assert.NotNull(root.Certificate);
            Assert.NotNull(root.PemKeyPath);
            Assert.NotNull(root.PemCertPath);
            Assert.NotNull(root.CrtPath);
            Assert.NotNull(root.PfxPath);
        }
    }
}