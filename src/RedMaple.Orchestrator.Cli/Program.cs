using RedMaple.Orchestrator.Cli.Commands;

using Spectre.Console.Cli;

namespace RedMaple.Orchestrator.Cli
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("red");
                //config.ValidateExamples();
                config.AddExample("secrets", "get", "my-secret", "-f", "file.bin");
                config.AddExample("secrets", "get", "my-secret", "-f", "file.bin", "-s", "http://172.18.20.21", "-k", "aes-key-as-hex/aes-iv-as-hex");

                // Run
                config.AddBranch<SecretsSettings>("secrets", add =>
                {
                    add.AddCommand<GetSecretCommand>("get");
                });
            });

            return app.Run(args);

        }
    }
}
