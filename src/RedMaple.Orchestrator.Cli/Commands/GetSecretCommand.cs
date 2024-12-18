using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Spectre.Console.Cli;

using Spectre.Console;
using RedMaple.Orchestrator.Sdk;

namespace RedMaple.Orchestrator.Cli.Commands
{
    public class GetSecretCommand : AsyncCommand<GetSecretCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, GetSecretCommand.Settings settings)
        {
            if(settings.SecretId is null)
            {
                return -1;
            }

            var client = new SecretsClient(settings.ServerUrl, settings.DecryptionKey);
            var secret = await client.GetSecretAsync(settings.SecretId);
            
            if(!string.IsNullOrEmpty(settings.DestinationFile))
            {
                await File.WriteAllBytesAsync(settings.DestinationFile, secret);
            }
            else
            {
                Console.Write(Encoding.UTF8.GetString(secret));
            }

            return 0;
        }

        public sealed class Settings : SecretsSettings
        {
            [CommandArgument(0, "<SECRET-ID>")]
            public string? SecretId { get; set; }

            [CommandOption("-k|--key")]
            public string? DecryptionKey { get; init; }

            [CommandOption("-f|--file")]
            public string? DestinationFile { get; init; }

        }


    }

}
