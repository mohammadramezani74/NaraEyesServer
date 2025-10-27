using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Bulkoperations
{
    public class GroupedDeviceViewModel
    {
        public Guid Id { get; set; }
        public string Ip { get; set; } = null!;
        public string? SerialNo { get; set; }
        public string? Model { get; set; } 
        public string? Branch { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? supervisionId { get; set; }
        public bool IsMarked { get; set; }
        public string? Supervisor { get; set; }
    }
}
