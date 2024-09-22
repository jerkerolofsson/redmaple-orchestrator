using RedMaple.Orchestrator.Contracts.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class VolumeBind
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the volume
        /// </summary>
        public string Name { get; set; } = "";
        public VolumeResource? Volume { get; set; }
        public required string ContainerPath { get; set; }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if(obj is VolumeBind other)
            {
                return Id.Equals(other.Id);
            }
            return false;
        }
    }
}
