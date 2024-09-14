using RedMaple.Orchestrator.DockerCompose;

namespace DockerComposeTests
{
    public class ParsingTests
    {
        [Fact]
        public void ParseFile_VolumesWithShortFormatAndAccessMode()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_short_format.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service?.volumes);
            Assert.Single(service.volumes);
            Assert.Equal("/etc/certs/https.pfx", service.volumes[0].Source);
            Assert.Equal("/certs/https.pfx", service.volumes[0].Target);
            Assert.Equal("ro", service.volumes[0].AccessMode);
        }
        [Fact]
        public void ParseFile_VolumesWithShortFormatNoAccessMode()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_short_format_no_access_mode.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service?.volumes);
            Assert.Single(service.volumes);
            Assert.Equal("/etc/certs/https.pfx", service.volumes[0].Source);
            Assert.Equal("/certs/https.pfx", service.volumes[0].Target);
            Assert.Equal("rw", service.volumes[0].AccessMode);
        }
    }
}