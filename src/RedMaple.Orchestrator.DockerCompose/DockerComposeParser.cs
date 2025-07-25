using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerComposeParser
    {
        private static readonly Regex _regex = new Regex("\\$\\{.*?\\}");
        
        public static string ReplaceEnvironmentVariables(string text, Dictionary<string,string?> env)
        {
            foreach (var envVar in env)
            {
                if (envVar.Value is not null)
                {
                    text = text.Replace("${" + envVar.Key + "}", envVar.Value);
                }
            }
            return text;
        }

        public static List<string> FindEnvironmentVariables(string text)
        {
            var variables = new List<string>();

            var matches = _regex.Matches(text);
            foreach (Match match in matches)
            {
                var name = match.Value[2..^1];
                variables.Add(name);
            }

            return variables;
        }

        public static DockerComposePlan ParseFile(string filename)
        {
            ArgumentException.ThrowIfNullOrEmpty(filename);
            var fileInfo = new FileInfo(filename);
            if (!fileInfo.Exists)
            {
                throw new ArgumentException("File does not exist");
            }
            var projectName = fileInfo.Directory?.Name ?? "unknown";

            var yaml = File.ReadAllText(filename);
            var plan = ParseYaml(yaml);
            plan.Yaml = yaml;
            plan.name = projectName;
            AddGlobalLabels(fileInfo, projectName, yaml, plan);

            return plan;
        }

        private static void AddGlobalLabels(FileInfo fileInfo, string projectName, string yaml, DockerComposePlan plan)
        {
            plan.Labels.Add(DockerComposeConstants.LABEL_PROJECT, projectName);
            plan.Labels.Add("com.docker.compose.project.config-files", fileInfo.FullName);
            if (fileInfo.DirectoryName is not null)
            {
                plan.Labels.Add("com.docker.compose.project.working_dir", fileInfo.DirectoryName);
            }
            plan.Labels.Add("com.docker.compose.config-hash", Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(yaml))));
        }

        public static string ToYaml(DockerComposePlan plan)
        {
            var serializer = new SerializerBuilder()
                .WithTypeConverter(new EnvironmentVariablesTypeConverter())
                .WithTypeConverter(new ServiceVolumeTypeConverter())
                .WithTypeConverter(new CommandConverter())
                .WithTypeConverter(new PortMappingTypeConverter())
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitNull)
                .Build();

            return serializer.Serialize(plan);
        }
        public static DockerComposePlan ParseYaml(string text, string? projectName)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new EnvironmentVariablesTypeConverter())
                .WithTypeConverter(new ServiceVolumeTypeConverter())
                .WithTypeConverter(new CommandConverter())
                .WithTypeConverter(new PortMappingTypeConverter())
                .IgnoreUnmatchedProperties() // don't throw an exception if there are unknown properties
                .Build();

            var dockerComposePlan = deserializer.Deserialize<DockerComposePlan>(text);
            if (dockerComposePlan is not null)
            { 
                dockerComposePlan.RequiredEnvironmentVariables = FindEnvironmentVariables(text);

                dockerComposePlan.Yaml = text;
                if(projectName is null)
                {
                    projectName = 
                        dockerComposePlan.services.Values.FirstOrDefault()?.container_name ??
                        dockerComposePlan.services.Keys.FirstOrDefault();
                }
                dockerComposePlan.name = projectName;
            }

            return dockerComposePlan ??new();
        }
    }
}
