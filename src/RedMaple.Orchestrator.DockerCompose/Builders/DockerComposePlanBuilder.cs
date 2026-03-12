namespace RedMaple.Orchestrator.DockerCompose.Builders
{
    public class DockerComposePlanBuilder
    {
        private readonly DockerComposePlan _dockerComposePlan;
        private IProgress<string> _callback = new NullProgress();
        private string? _environmentFile = null;

        private class NullProgress : IProgress<string>
        {
            public void Report(string value) {}
        }

        private DockerComposePlanBuilder(DockerComposePlan dockerComposePlan)
        {
            _dockerComposePlan = dockerComposePlan;
        }

        /// <summary>
        /// Starts the Docker containers defined in the Docker Compose plan.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation of starting the containers.</returns>
        public async Task UpAsync(CancellationToken cancellationToken = default)
        {
            var dockerCompose = DockerComposeFactory.Create();
            await dockerCompose.UpAsync(_callback, _dockerComposePlan, _environmentFile, cancellationToken);
        }

        /// <summary>
        /// Stops and removes containers defined in the Docker Compose plan.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task DownAsync(CancellationToken cancellationToken = default)
        {
            var dockerCompose = DockerComposeFactory.Create();
            await dockerCompose.DownAsync(_callback, _dockerComposePlan, _environmentFile, cancellationToken);
        }

        /// <summary>
        /// Sets a progress callback to receive updates during the Docker Compose operation.
        /// </summary>
        /// <remarks>This method allows the caller to specify a callback that will be invoked with
        /// progress updates as the Docker Compose operation proceeds.</remarks>
        /// <param name="callback">The callback to invoke with progress messages, providing real-time feedback on the operation's status.</param>
        /// <returns>The current instance of the DockerComposePlanBuilder, allowing for method chaining.</returns>
        public DockerComposePlanBuilder WithProgressCallback(IProgress<string> callback)
        {
            _callback = callback;
            return this;
        }

        /// <summary>
        /// Sets the path to an .env file where environment variables can be set
        /// </summary>
        /// <param name="environmentFile"></param>
        /// <returns></returns>
        public DockerComposePlanBuilder WithEnvironmentFile(string environmentFile)
        {
            _environmentFile = environmentFile;
            return this;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DockerComposePlanBuilder"/> using the specified Docker Compose
        /// file.
        /// </summary>
        /// <remarks>Ensure that the provided file path points to a valid Docker Compose file. This method
        /// will throw an exception if the file cannot be parsed or accessed.</remarks>
        /// <param name="dockerComposeFilePath">The path to the Docker Compose file to be parsed. The file must exist and be accessible; otherwise, an
        /// exception will be thrown.</param>
        /// <returns>A <see cref="DockerComposePlanBuilder"/> initialized with the parsed contents of the specified Docker
        /// Compose file.</returns>
        public static DockerComposePlanBuilder FromFile(string dockerComposeFilePath)
        {
            DockerComposePlanBuilder builder = new DockerComposePlanBuilder(DockerComposeParser.ParseFile(dockerComposeFilePath));
            return builder;
        }

        /// <summary>
        /// Creates a new instance of the DockerComposePlanBuilder from the specified Docker Compose YAML string.
        /// </summary>
        /// <remarks>Ensure that the provided YAML string adheres to the Docker Compose file format.
        /// Invalid YAML may result in parsing errors.</remarks>
        /// <param name="dockerComposeYaml">The Docker Compose YAML configuration as a string. This string must be a valid YAML format representing the
        /// Docker Compose configuration.</param>
        /// <returns>A DockerComposePlanBuilder instance initialized with the parsed Docker Compose configuration.</returns>
        public static DockerComposePlanBuilder FromYaml(string dockerComposeYaml)
        {
            DockerComposePlanBuilder builder = new DockerComposePlanBuilder(DockerComposeParser.ParseYaml(dockerComposeYaml));
            return builder;
        }
    }
}
