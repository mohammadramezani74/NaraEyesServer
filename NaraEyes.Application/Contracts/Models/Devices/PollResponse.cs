using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class PollResponse
    {
        public DateTime ServerTime { get; set; } = DateTime.Now;
        public List<OutBoxDeviceMessage> Commands { get; set; } = new();
    }
}
