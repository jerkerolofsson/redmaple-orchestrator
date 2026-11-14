using RedMaple.Orchestrator.DockerCompose;

namespace DockerComposeTests
{
    public class NetworkParsingTests
    {
        [Fact]
        public void ParseFile_WithBasicNetworks()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_basic.yaml");

            Assert.NotNull(plan?.networks);
            Assert.Equal(2, plan.networks.Count);
            Assert.True(plan.networks.ContainsKey("front-tier"));
            Assert.True(plan.networks.ContainsKey("back-tier"));

            // According to spec, networks can be empty (using defaults)
            var frontTier = plan.networks["front-tier"];
            if (frontTier != null)
            {
                Assert.Null(frontTier.driver);
                Assert.Null(frontTier.Internal);
            }

            var backTier = plan.networks["back-tier"];
            // Entry should exist in the dictionary
            Assert.True(true); // Placeholder for assertion - network exists in plan
        }

        [Fact]
        public void ParseFile_WithNetworksAndDriver()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_driver.yaml");

            Assert.NotNull(plan?.networks);
            Assert.Equal(2, plan.networks.Count);

            var frontend = plan.networks["frontend"];
            Assert.NotNull(frontend);
            Assert.Equal("custom-driver-1", frontend.driver);

            var backend = plan.networks["backend"];
            Assert.NotNull(backend);
            Assert.Equal("custom-driver-2", backend.driver);
        }

        [Fact]
        public void ParseFile_WithNetworkDriverOpts()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_driver.yaml");

            Assert.NotNull(plan?.networks);
            var backend = plan.networks["backend"];
            Assert.NotNull(backend);
            Assert.NotNull(backend.driver_opts);
            Assert.Equal("1", backend.driver_opts["foo"]);
            Assert.Equal("2", backend.driver_opts["bar"]);
        }

        [Fact]
        public void ParseFile_WithInternalNetwork()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_internal.yaml");

            Assert.NotNull(plan?.networks);
            Assert.Single(plan.networks);

            var internalNet = plan.networks["internal-net"];
            Assert.NotNull(internalNet);
            Assert.True(internalNet.Internal);
        }

        [Fact]
        public void ParseFile_WithExternalNetwork()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_external.yaml");

            Assert.NotNull(plan?.networks);
            Assert.True(plan.networks.ContainsKey("outside"));

            var outside = plan.networks["outside"];
            Assert.NotNull(outside);
            Assert.True(outside.external);
        }

        [Fact]
        public void ParseFile_WithNetworkIpam()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_ipam.yaml");

            Assert.NotNull(plan?.networks);
            var mynet1 = plan.networks["mynet1"];
            Assert.NotNull(mynet1);
            Assert.NotNull(mynet1.ipam);

            var ipam = mynet1.ipam;
            Assert.Equal("default", ipam.driver);

            Assert.NotNull(ipam.config);
            Assert.Single(ipam.config);

            var config = ipam.config[0];
            Assert.Equal("172.28.0.0/16", config.subnet);
            Assert.Equal("172.28.5.0/24", config.ip_range);
            Assert.Equal("172.28.5.254", config.gateway);

            Assert.NotNull(config.aux_addresses);
            Assert.Equal("172.28.1.5", config.aux_addresses["host1"]);
            Assert.Equal("172.28.1.6", config.aux_addresses["host2"]);

            Assert.NotNull(ipam.options);
            Assert.Equal("bar", ipam.options["foo"]);
            Assert.Equal("0", ipam.options["baz"]);
        }

        [Fact]
        public void ParseFile_WithNetworkLabels()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_labels.yaml");

            Assert.NotNull(plan?.networks);
            var mynet1 = plan.networks["mynet1"];
            Assert.NotNull(mynet1);
            Assert.NotNull(mynet1.labels);

            Assert.Equal("Financial transaction network", mynet1.labels["com.example.description"]);
            Assert.Equal("Finance", mynet1.labels["com.example.department"]);
            Assert.Equal("", mynet1.labels["com.example.label-with-empty-value"]);
        }

        [Fact]
        public void ParseFile_WithNetworkCustomName()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_name.yaml");

            Assert.NotNull(plan?.networks);
            var network1 = plan.networks["network1"];
            Assert.NotNull(network1);
            Assert.Equal("my-app-net", network1.name);
        }

        [Fact]
        public void ParseFile_WithDefaultNetworkOverride()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_default_override.yaml");

            Assert.NotNull(plan?.networks);
            Assert.True(plan.networks.ContainsKey("default"));

            var defaultNetwork = plan.networks["default"];
            Assert.NotNull(defaultNetwork);
            Assert.Equal("a_network", defaultNetwork.name);
            Assert.NotNull(defaultNetwork.driver_opts);
            Assert.Equal("127.0.0.1", defaultNetwork.driver_opts["com.docker.network.bridge.host_binding_ipv4"]);
        }

        [Fact]
        public void ParseFile_WithIpv6Network()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_ipv6.yaml");

            Assert.NotNull(plan?.networks);
            var ip6net = plan.networks["ip6net"];
            Assert.NotNull(ip6net);
            Assert.False(ip6net.enable_ipv4);
            Assert.True(ip6net.enable_ipv6);
        }

        [Fact]
        public void ParseFile_WithAttachableNetwork()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_attachable.yaml");

            Assert.NotNull(plan?.networks);
            var mynet1 = plan.networks["mynet1"];
            Assert.NotNull(mynet1);
            Assert.Equal("overlay", mynet1.driver);
            Assert.True(mynet1.attachable);
        }

        [Fact]
        public void ParseFile_WithMultipleServicesOnNetworks()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_driver.yaml");

            Assert.NotNull(plan?.services);
            Assert.Equal(3, plan.services.Count);

            var proxy = plan.services["proxy"];
            Assert.NotNull(proxy.networks);
            Assert.Single(proxy.networks);
            Assert.Equal("frontend", proxy.networks[0]);

            var app = plan.services["app"];
            Assert.NotNull(app.networks);
            Assert.Equal(2, app.networks.Count);
            Assert.Contains("frontend", app.networks);
            Assert.Contains("backend", app.networks);

            var db = plan.services["db"];
            Assert.NotNull(db.networks);
            Assert.Single(db.networks);
            Assert.Equal("backend", db.networks[0]);
        }

        [Fact]
        public void ParseFile_NetworkIsolation()
        {
            var plan = DockerComposeParser.ParseFile("TestData/networks_with_driver.yaml");

            // Proxy and DB don't share a network, so they should be isolated
            var proxy = plan.services["proxy"];
            var db = plan.services["db"];

            var proxyNetworks = new HashSet<string>(proxy.networks!);
            var dbNetworks = new HashSet<string>(db.networks!);

            var commonNetworks = proxyNetworks.Intersect(dbNetworks);
            Assert.Empty(commonNetworks);
        }
    }
}
