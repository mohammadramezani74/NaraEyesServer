using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public sealed class BranchErrorAggDto
    {
        
        public string? BranchName { get; set; }
        public int ErrorModules { get; set; }
        public int AffectedDevices { get; set; }
    }
}
