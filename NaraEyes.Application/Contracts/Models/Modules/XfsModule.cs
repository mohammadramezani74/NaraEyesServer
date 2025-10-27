using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules
{
    public class XfsModule
    {
        public Guid ModuleId { get; set; }
        public XfsModule(string name, string desc, HealthStatus status, string statusDesc) { Name = name; Description = desc; HealthStatus = status; Status = statusDesc; }
        public string? Name { get; set; }
        public string Description { get; set; }
        public string Status { get;set; }
        public HealthStatus HealthStatus{get;set;}
    }
}
