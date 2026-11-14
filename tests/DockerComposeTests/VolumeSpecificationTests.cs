using RedMaple.Orchestrator.DockerCompose;

namespace DockerComposeTests
{
    public class VolumeSpecificationTests
    {
        [Fact]
        public void ParseFile_WithBasicVolume()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_basic.yaml");

            Assert.NotNull(plan?.volumes);
            Assert.Single(plan.volumes);
            Assert.True(plan.volumes.ContainsKey("db-data"));

            // According to spec: "An entry under the top-level volumes section can be empty,
            // in which case it uses the container engine's default configuration"
            var dbData = plan.volumes["db-data"];
            // The volume may be null (if empty in YAML) or an object with null driver
            if (dbData != null)
            {
                Assert.Null(dbData.driver);
                Assert.Null(dbData.external);
            }
        }

        [Fact]
        public void ParseFile_WithVolumeDriver()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_driver.yaml");

            Assert.NotNull(plan?.volumes);
            Assert.Single(plan.volumes);

            var exampleVol = plan.volumes["example-vol"];
            Assert.NotNull(exampleVol);
            Assert.Equal("foobar", exampleVol.driver);
        }

        [Fact]
        public void ParseFile_WithVolumeDriverOpts()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_driver_opts.yaml");

            Assert.NotNull(plan?.volumes);
            var exampleVol = plan.volumes["example-vol"];
            Assert.NotNull(exampleVol);
            Assert.NotNull(exampleVol.driver_opts);

            var opts = exampleVol.driver_opts;
            // DockerComposeVolumeDriverOpts is a Dictionary<string, string>
            Assert.Equal("nfs", opts["type"]);
            Assert.Equal("addr=10.40.0.199,nolock,soft,rw", opts["o"]);
            Assert.Equal(":/docker/example", opts["device"]);
        }

        [Fact]
        public void ParseFile_WithExternalVolume()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_external.yaml");

            Assert.NotNull(plan?.volumes);
            var dbData = plan.volumes["db-data"];
            Assert.NotNull(dbData);
            Assert.True(dbData.external);
        }

        [Fact]
        public void ParseFile_WithVolumeLabels()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_labels.yaml");

            Assert.NotNull(plan?.volumes);
            var dbData = plan.volumes["db-data"];
            Assert.NotNull(dbData);
            Assert.NotNull(dbData.labels);
            // labels is a List<string>, according to YAML spec
            // Format: "key=value" or just "key" for empty value
            Assert.Contains("com.example.description=Database volume", dbData.labels);
            Assert.Contains("com.example.department=IT/Ops", dbData.labels);
        }

        [Fact]
        public void ParseFile_WithVolumeName()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_name.yaml");

            Assert.NotNull(plan?.volumes);
            var dbData = plan.volumes["db-data"];
            Assert.NotNull(dbData);
            Assert.Equal("my-app-data", dbData.name);
        }

        [Fact]
        public void ParseFile_WithMultipleServicesUsingVolume()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_basic.yaml");

            Assert.NotNull(plan?.services);
            Assert.Equal(2, plan.services.Count);

            var backend = plan.services["backend"];
            Assert.NotNull(backend.volumes);
            Assert.Single(backend.volumes);
            Assert.Equal("db-data", backend.volumes[0].Source);
            Assert.Equal("/etc/data", backend.volumes[0].Target);

            var backup = plan.services["backup"];
            Assert.NotNull(backup.volumes);
            Assert.Single(backup.volumes);
            Assert.Equal("db-data", backup.volumes[0].Source);
            Assert.Equal("/var/lib/backup/data", backup.volumes[0].Target);
        }

        [Fact]
        public void ParseFile_VolumeMountPaths()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_basic.yaml");

            var backend = plan.services["backend"];
            Assert.Equal("/etc/data", backend.volumes[0].Target);

            var backup = plan.services["backup"];
            Assert.Equal("/var/lib/backup/data", backup.volumes[0].Target);
        }

        [Fact]
        public void ParseFile_VolumeDriverIsConfigurable()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_driver_opts.yaml");

            var vol = plan.volumes["example-vol"];
            Assert.NotNull(vol.driver_opts);
            // NFS volume with specific options
            Assert.Equal("nfs", vol.driver_opts["type"]);
        }

        [Fact]
        public void ParseFile_VolumeNamingWithCustomName()
        {
            // When name is set, the compose file reference and actual volume name can differ
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_name.yaml");

            var dbData = plan.volumes["db-data"];
            Assert.Equal("my-app-data", dbData.name);

            // In the compose file, it's referenced as db-data, but created as my-app-data
        }

        [Fact]
        public void ParseFile_ExternalVolumeDoesNotRequireDriver()
        {
            var plan = DockerComposeParser.ParseFile("TestData/volumes_external.yaml");

            var dbData = plan.volumes["db-data"];
            Assert.True(dbData.external);
            // When external is true, other attributes are only for metadata
            Assert.Null(dbData.driver);
        }

        [Fact]
        public void ParseFile_VolumeWithAllAttributes()
        {
            // volumes_with_driver.yaml has driver specified
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_driver.yaml");
            var vol = plan.volumes["example-vol"];

            // Basic volume attributes
            Assert.NotNull(vol);
            Assert.Equal("foobar", vol.driver);
        }

        [Fact]
        public void ParseFile_BasicVolumeCreation()
        {
            // According to spec: "When the services are run, the volume is created 
            // if it doesn't already exist. Otherwise, the existing volume is used."
            var plan = DockerComposeParser.ParseFile("TestData/volumes_basic.yaml");

            Assert.NotNull(plan?.volumes);
            Assert.Single(plan.volumes);

            var dbData = plan.volumes["db-data"];
            // The volume may be null (if empty in YAML) or have null driver
            if (dbData != null)
            {
                // Null driver means it uses the container engine's default (usually 'local')
                Assert.Null(dbData.driver);
            }
        }

        [Fact]
        public void ParseFile_VolumeDriverOptsArePlatformSpecific()
        {
            // driver_opts are driver-dependent and should be passed as-is
            var plan = DockerComposeParser.ParseFile("TestData/volumes_with_driver_opts.yaml");

            var vol = plan.volumes["example-vol"];
            Assert.NotNull(vol.driver_opts);

            // These are NFS-specific options - DockerComposeVolumeDriverOpts is Dictionary<string,string>
            Assert.NotNull(vol.driver_opts["o"]);
            Assert.NotNull(vol.driver_opts["device"]);
        }
    }
}
