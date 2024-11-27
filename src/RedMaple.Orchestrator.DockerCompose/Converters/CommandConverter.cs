using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Docker.DotNet.Models;

using YamlDotNet.Core;

namespace RedMaple.Orchestrator.DockerCompose.Converters
{
    public class Command : List<string>;

    public class CommandConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Command);
        }

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            if (parser.TryConsume<SequenceStart>(out _))
            {
                var command = new Command();
                while (parser.TryConsume<Scalar>(out var scalar))
                {
                    command.Add(scalar.Value);
                }
                if(!parser.TryConsume< SequenceEnd>(out _))
                {
                    // 
                }
                return command;
            }
            else
            {
                if (parser.TryConsume<Scalar>(out var scalar))
                {
                    var command = new Command();
                    command.AddRange(scalar.Value.Split());
                    return command;
                }
            }
            return null;

        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, SequenceStyle.Block));

            if (value is Command list)
            {
                foreach (var item in list)
                {
                    emitter.Emit(new Scalar(item));
                }
            }
            emitter.Emit(new SequenceEnd());
        }
    }
}
