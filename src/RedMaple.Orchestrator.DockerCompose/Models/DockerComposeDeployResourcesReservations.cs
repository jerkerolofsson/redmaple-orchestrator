﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// </summary>
    public class DockerComposeDeployResourcesReservations
    {
        public List<DockerComposeDeviceReservation>? devices { get; set; }
    }
}