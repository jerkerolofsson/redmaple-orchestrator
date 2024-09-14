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

    public class DockerComposePortMappingTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(List<DockerComposePortMapping>);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume<SequenceStart>(out _))
            {
                return ParseList(parser); // We're parsing a YAML array
            }

            throw new InvalidOperationException("Expected a YAML object or array");
        }

        private static void ParseLongFormat(IParser parser, DockerComposePortMapping port)
        {
            // Read all the key-value pairs until we reached the end of the YAML object
            while (!parser.Accept<MappingEnd>(out _))
            {
                var key = parser.Consume<Scalar>();
                var value = parser.Consume<Scalar>();

                switch(key.Value)
                {
                    case "published":
                        if (int.TryParse(value.Value, out int hostPort))
                        {
                            port.HostPort = hostPort;
                        }
                        break;
                    case "target":
                        if (int.TryParse(value.Value, out int containerPort))
                        {
                            port.ContainerPort = containerPort;
                        }
                        break;
                    case "host_ip":
                        port.HostIp = value.Value;
                        break;
                    case "mode":
                        port.Mode = value.Value;
                        break;
                    case "protocol":
                        port.Protocol = value.Value;
                        break;
                    case "name":
                        port.Name = value.Value;
                        break;
                    case "app_protocol":
                        port.AppProtocol = value.Value;
                        break;
                }
                //envvars[key.Value] = value.Value;
            }

            // Consume the mapping end token
            parser.MoveNext();
        }

        private static List<DockerComposePortMapping> ParseList(IParser parser)
        {
            var ports = new List<DockerComposePortMapping>();

            // Read all the array values until we reach the end of the YAML array
            while (!parser.Accept<SequenceEnd>(out _))
            {
                var port = new DockerComposePortMapping();
                ports.Add(port);

                if (parser.TryConsume<Scalar>(out var scalar))
                {
                    ParseShortFormat(scalar, port);
                }
                else if (parser.TryConsume<MappingStart>(out var mapping))
                {
                    ParseLongFormat(parser, port);
                }
            }
            // Consume the mapping end token
            parser.MoveNext();
            return ports;
        }

        private static void ParseShortFormat(Scalar scalar, DockerComposePortMapping port)
        {
            var portMapping = scalar.Value.Split('/');

            var portsString = portMapping[0];
            if (portMapping.Length >= 2)
            {
                port.Protocol = portMapping[1];
            }

            var items = portsString.Split(':');
            if (items.Length == 1)
            {
                if (int.TryParse(items[0], out int containerPort))
                {
                    port.ContainerPort = containerPort;
                }
            }
            else if (items.Length == 2)
            {
                if (int.TryParse(items[0], out int hostPort))
                {
                    port.HostPort = hostPort;
                }
                if (int.TryParse(items[1], out int containerPort))
                {
                    port.ContainerPort = containerPort;
                }
            }
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            var ports = (List<DockerComposePortMapping>)value!;

            // We start a new YAML object
            emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, SequenceStyle.Block));

            foreach (var port in ports)
            {
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
                if (port.Name is not null)
                {
                    emitter.Emit(new Scalar("name"));
                    emitter.Emit(new Scalar(port.Name));
                }
                if (port.HostIp is not null)
                {
                    emitter.Emit(new Scalar("host_ip"));
                    emitter.Emit(new Scalar(port.HostIp));
                }
                if (port.AppProtocol is not null)
                {
                    emitter.Emit(new Scalar("app_protocol"));
                    emitter.Emit(new Scalar(port.AppProtocol));
                }
                if (port.Mode is not null)
                {
                    emitter.Emit(new Scalar("mode"));
                    emitter.Emit(new Scalar(port.Mode));
                }
                if (port.HostPort is not null)
                {
                    emitter.Emit(new Scalar("published"));
                    emitter.Emit(new Scalar(port.HostPort.ToString()!));
                }
                if (port.ContainerPort is not null)
                {
                    emitter.Emit(new Scalar("target"));
                    emitter.Emit(new Scalar(port.ContainerPort.ToString()!));
                }
                if (port.Protocol is not null)
                {
                    emitter.Emit(new Scalar("protocol"));
                    emitter.Emit(new Scalar(port.Protocol));
                }
                emitter.Emit(new MappingEnd());

            }

            // We end the YAML object
            emitter.Emit(new SequenceEnd());
        }

    }
}
