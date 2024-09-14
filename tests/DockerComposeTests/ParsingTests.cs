using RedMaple.Orchestrator.DockerCompose;

namespace DockerComposeTests
{
    public class ParsingTests
    {
        [Fact]
        public void ParseFile_WithLongPortFormat()
        {
            var plan = DockerComposeParser.ParseFile("TestData/ports_long_format.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service.ports);
            Assert.Single(service.ports);
            Assert.Equal(1234, service.ports[0].HostPort);
            Assert.Equal(8081, service.ports[0].ContainerPort);
            Assert.Equal("http port", service.ports[0].Name);
        }

        [Fact]
        public void ParseFile_WithShortPortFormatProtocolUdp()
        {
            var plan = DockerComposeParser.ParseFile("TestData/ports_short_format_udp.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service.ports);
            Assert.Single(service.ports);
            Assert.Equal(1234, service.ports[0].HostPort);
            Assert.Equal(8081, service.ports[0].ContainerPort);
            Assert.Equal("UDP", service.ports[0].Protocol);
        }

        [Fact]
        public void ParseFile_WithShortPortFormatProtocolTdp()
        {
            var plan = DockerComposeParser.ParseFile("TestData/ports_short_format_tcp.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service.ports);
            Assert.Single(service.ports);
            Assert.Equal(1234, service.ports[0].HostPort);
            Assert.Equal(8081, service.ports[0].ContainerPort);
            Assert.Equal("TCP", service.ports[0].Protocol);
        }

        [Fact]
        public void ParseFile_WithShortPortFormat()
        {
            var plan = DockerComposeParser.ParseFile("TestData/ports_short_format.yaml");
            Assert.NotNull(plan?.services);
            Assert.Single(plan.services);

            var service = plan.services["test"]!;

            Assert.NotNull(service.ports);
            Assert.Single(service.ports);
            Assert.Equal(1234, service.ports[0].HostPort);
            Assert.Equal(8081, service.ports[0].ContainerPort);
        }
    }
}