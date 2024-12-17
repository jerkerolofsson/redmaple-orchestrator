using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Secrets
{
    /// <summary>
    /// Server or similar
    /// </summary>
    public class Secret
    {
        /// <summary>
        /// ID
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Slug
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// ID of key used to encrypt the secret
        /// </summary>
        public required string Key { get; set; }

        /// <summary>
        /// Value of secret, saved as base64
        /// </summary>
        public string? Value { get; set; }
    }
}
