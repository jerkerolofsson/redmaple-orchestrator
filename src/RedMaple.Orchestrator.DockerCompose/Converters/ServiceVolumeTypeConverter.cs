using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RedMaple.Orchestrator.DockerCompose.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace RedMaple.Orchestrator.DockerCompose.Converters
{

    public class ServiceVolumeTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(List<DockerComposeServiceVolume>);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume<SequenceStart>(out _))
            {
                return ParseList(parser); // We're parsing a YAML array
            }

            throw new InvalidOperationException("Expected a YAML object or array");
        }

        /// <summary>
        /// https://docs.docker.com/reference/compose-file/services/
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="volume"></param>
        private static void ParseLongFormat(IParser parser, DockerComposeServiceVolume volume)
        {
            // Read all the key-value pairs until we reached the end of the YAML object
            while (!parser.Accept<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>();
                if(parser.TryConsume<Scalar>(out Scalar? value))
                {
                    switch (key.Value)
                    {
                        case "type":
                            volume.Type = value.Value;
                            break;
                        case "source":
                            volume.Source = value.Value;
                            break;
                        case "target":
                            volume.Target = value.Value;
                            break;
                        case "read_only":
                            volume.ReadOnly = value.Value;
                            break;
                        case "consistency":
                            volume.Consistency = value.Value;
                            break;
                    }
                }
            }

            // Consume the mapping end token
            parser.MoveNext();
        }

        private static List<DockerComposeServiceVolume> ParseList(IParser parser)
        {
            var volumes = new List<DockerComposeServiceVolume>();

            // Read all the array values until we reach the end of the YAML array
            while (!parser.Accept<SequenceEnd>(out _))
            {
                var volume = new DockerComposeServiceVolume();
                volumes.Add(volume);

                if (parser.TryConsume<Scalar>(out var scalar))
                {
                    ParseShortFormat(scalar, volume);
                }
                else if (parser.TryConsume<MappingStart>(out var mapping))
                {
                    ParseLongFormat(parser, volume);
                }
            }
            // Consume the mapping end token
            parser.MoveNext();
            return volumes;
        }

        /// <summary>
        /// /host/path:/container/path:ro"
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="volume"></param>
        private static void ParseShortFormat(Scalar scalar, DockerComposeServiceVolume volume)
        {
            var items = scalar.Value.Split(':');

            // As windows may use : in the path, we must detect
            // if the access mode is present
            var accessMode = items[^1];

            if (accessMode == "z" ||
                accessMode == "Z" ||
                accessMode == "ro" ||
                accessMode == "rw")
            {
                // We have access mode
                var pAccessMode = scalar.Value.LastIndexOf(':');
                var p = scalar.Value.LastIndexOf(':', pAccessMode-1);
                if(p == -1)
                {
                    throw new Exception("Invalid short format: " + scalar.Value);
                }

                volume.Source = scalar.Value[0..p];
                volume.Target = scalar.Value[(p + 1)..(pAccessMode)];
                volume.AccessMode = accessMode;
            }
            else
            {
                var p = scalar.Value.LastIndexOf(':');
                volume.Source = scalar.Value[0..p];
                volume.Target = scalar.Value[(p+1)..];
                volume.AccessMode = "rw";
            }
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            if (value is null)
            {
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, SequenceStyle.Block));
                emitter.Emit(new SequenceEnd());
                return;
            }
            var volumes = (List<DockerComposeServiceVolume>)value!;

            // We start a new YAML object
            emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, SequenceStyle.Block));

            foreach (var volume in volumes)
            {
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
                if (volume.Type is not null)
                {
                    emitter.Emit(new Scalar("type"));
                    emitter.Emit(new Scalar(volume.Type));
                }
                if (volume.Source is not null)
                {
                    emitter.Emit(new Scalar("source"));
                    emitter.Emit(new Scalar(volume.Source));
                }
                if (volume.Target is not null)
                {
                    emitter.Emit(new Scalar("target"));
                    emitter.Emit(new Scalar(volume.Target));
                }
                if (volume.Consistency is not null)
                {
                    emitter.Emit(new Scalar("consistency"));
                    emitter.Emit(new Scalar(volume.Consistency));
                }
                if (volume.ReadOnly is not null)
                {
                    emitter.Emit(new Scalar("read_only"));
                    emitter.Emit(new Scalar(volume.ReadOnly));
                }
                emitter.Emit(new MappingEnd());

            }

            // We end the YAML object
            emitter.Emit(new SequenceEnd());
        }

    }
}
