using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace RedMaple.Orchestrator.DockerCompose.Converters
{
    /// <summary>
    /// https://anthonysimmon.com/yamldotnet-custom-type-converters/
    /// </summary>
    public class EnvironmentVariables : Dictionary<string, string>
    {
    }

    public class EnvironmentVariablesTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(EnvironmentVariables);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume<MappingStart>(out _))
            {
                return ParseMapping(parser); // We're parsing a YAML object
            }

            if (parser.TryConsume<SequenceStart>(out _))
            {
                return ParseSequence(parser); // We're parsing a YAML array
            }

            throw new InvalidOperationException("Expected a YAML object or array");
        }

        private static EnvironmentVariables ParseMapping(IParser parser)
        {
            var envvars = new EnvironmentVariables();

            // Read all the key-value pairs until we reached the end of the YAML object
            while (!parser.Accept<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>();
                var value = parser.Consume<Scalar>();
                envvars[key.Value] = value.Value;
            }

            // Consume the mapping end token
            parser.MoveNext();
            return envvars;
        }

        // Regex that parses a key-value pair in the array format (e.g. "FOO=BAR" or "FOO")
        private static readonly Regex EnvironmentVariableLineRegex = new Regex("^(?<key>[^=]*)(=(?<value>.*))?$", RegexOptions.Compiled);

        private static EnvironmentVariables ParseSequence(IParser parser)
        {
            var envvars = new EnvironmentVariables();

            // Read all the array values until we reach the end of the YAML array
            while (!parser.Accept<SequenceEnd>(out _))
            {
                var scalar = parser.Consume<Scalar>();

                if (EnvironmentVariableLineRegex.Match(scalar.Value) is { Success: true } match)
                {
                    var key = match.Groups["key"].Value;
                    var value = match.Groups["value"].Success ? match.Groups["value"].Value : string.Empty;
                    envvars[key] = value;
                }
                else
                {
                    throw new InvalidOperationException("Invalid key value mapping: " + scalar.Value);
                }
            }

            // Consume the mapping end token
            parser.MoveNext();
            return envvars;
        }

        private static readonly char[] KeyCharactersThatRequireQuotes = { ' ', '/', '\\', '~', ':', '$', '{', '}' };

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            var envvars = (EnvironmentVariables)value!;

            // We start a new YAML object
            emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));

            foreach (var entry in envvars)
            {
                // We try to determine if the value needs to be quoted if it contains special characters
                var keyScalar = entry.Key.IndexOfAny(KeyCharactersThatRequireQuotes) >= 0
                    ? new Scalar(AnchorName.Empty, TagName.Empty, entry.Key, ScalarStyle.DoubleQuoted, isPlainImplicit: false, isQuotedImplicit: true)
                    : new Scalar(AnchorName.Empty, TagName.Empty, entry.Key, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false);

                // Write the key, then the value
                emitter.Emit(keyScalar);
                emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, entry.Value, ScalarStyle.DoubleQuoted, isPlainImplicit: false, isQuotedImplicit: true));
            }

            // We end the YAML object
            emitter.Emit(new MappingEnd());
        }

    }
}
