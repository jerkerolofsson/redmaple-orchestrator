using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Spectre.Console.Cli;

namespace RedMaple.Orchestrator.Cli.Commands
{
    public class SecretsSettings : CommandSettings
    {
        [CommandOption("-s|--server")]
        public string? ServerUrl { get; init; }

    }
}
