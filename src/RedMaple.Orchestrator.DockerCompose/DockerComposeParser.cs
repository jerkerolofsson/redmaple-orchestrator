using RedMaple.Orchestrator.DockerCompose.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerComposeParser
    {
        private static readonly Regex _regex = new Regex("\\$\\{.*?\\}");

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
            var text = File.ReadAllText(filename);
            return ParseYaml(text);
        }

        public static DockerComposePlan ParseYaml(string text)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new EnvironmentVariablesTypeConverter())
                .WithTypeConverter(new DockerComposePortMappingTypeConverter())
                .IgnoreUnmatchedProperties() // don't throw an exception if there are unknown properties
                .Build();

            var dockerComposePlan = deserializer.Deserialize<DockerComposePlan>(text);
            return dockerComposePlan;
        }
    }
}
