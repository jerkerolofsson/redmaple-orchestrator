using RedMaple.Orchestrator.DockerCompose.Converters;
using RedMaple.Orchestrator.DockerCompose.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {
        private void ParseEnvironmentFile(string filename, Dictionary<string,string?> env)
        {
            foreach(var line in File.ReadAllLines(filename))
            {
                var p = line.IndexOf('=');
                if (p == -1)
                {
                    env[line.Trim()] = null;
                }
                else
                {
                    var key = line[0..p].Trim();
                    var val = line[(p+1)..].Trim();
                    env[key] = val;
                }
            }
        }

        /// <summary>
        /// https://docs.docker.com/compose/environment-variables/envvars-precedence/
        /// </summary>
        /// <param name="requiredEnvironmentVariables"></param>
        /// <param name="envFile"></param>
        /// <param name="env"></param>
        private void AssignEnvironmentVariables(
            IReadOnlyList<string> requiredEnvironmentVariables,
            EnvironmentVariables? service, 
            Dictionary<string, string?> envFile,
            Dictionary<string, string?> target,
            bool validate = true)
        {
            if (service is not null)
            {
                foreach (var key in service.Keys)
                {
                    target[key] = service[key];
                }
            }

            // Env file
            foreach (var key in envFile.Keys)
            {
                if (!target.ContainsKey(key) || target[key] == null)
                {
                    target[key] = envFile[key];
                }
            }

            // Host OS
            foreach (var key in requiredEnvironmentVariables)
            {
                if (!target.ContainsKey(key) || target[key] == null)
                {
                    target[key] = Environment.GetEnvironmentVariable(key);
                }
            }

            // .env (not supported)

            if (validate)
            {
                EnsureAllEnvironmentVariablesExist(requiredEnvironmentVariables, target);
            }
        }

        private IList<string> MapEnv(Dictionary<string, string?> env)
        {
            var list = new List<string>();
            foreach (var pair in env)
            {
                list.Add($"{pair.Key}={pair.Value}");
            }
            return list;
        }

        /// <summary>
        /// Throws an exception if an environment variable which is required (used in the compose file)
        /// is not set
        /// </summary>
        /// <param name="requiredEnvironmentVariables"></param>
        /// <param name="env"></param>
        /// <exception cref="InvalidDataException"></exception>
        private void EnsureAllEnvironmentVariablesExist(
            IReadOnlyList<string> requiredEnvironmentVariables,
            Dictionary<string, string?> env)
        {
            foreach (var key in requiredEnvironmentVariables)
            {
                if(!env.ContainsKey(key) || env[key] is null)
                {
                    throw new InvalidDataException($"The environment variable {key} is missing");
                }
            }
        }


        private Dictionary<string, string?> LoadEnvironmentFile(DockerComposePlan plan, string? environmentFile)
        {
            Dictionary<string, string?> envFile = new();
            if (environmentFile is not null)
            {
                var fileInfo = new FileInfo(environmentFile);
                if (!fileInfo.Exists)
                {
                    throw new ArgumentException($"The environment file '{environmentFile}' does not exist");
                }
                plan.Labels["com.docker.compose.project.environment_file"] = fileInfo.FullName;

                // Load and parse environment file
                ParseEnvironmentFile(fileInfo.FullName, envFile);
            }

            return envFile;
        }

    }
}
