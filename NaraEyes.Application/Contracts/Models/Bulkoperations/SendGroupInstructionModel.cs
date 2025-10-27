using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Bulkoperations
{
    public class SendGroupInstructionModel
    {
        public Guid MessageBoxId { get; set; }
        public Guid CampaignId { get; set; }
        public string?  Ip { get; set; }
        public int Type { get; set; }
        public string? url { get; set; }
    }
}
