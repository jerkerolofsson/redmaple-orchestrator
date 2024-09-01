﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    /// <summary>
    /// Information about a running node
    /// </summary>
    public class EnrollmentRequest
    {
        public required string Id { get; set; }
    }
}
