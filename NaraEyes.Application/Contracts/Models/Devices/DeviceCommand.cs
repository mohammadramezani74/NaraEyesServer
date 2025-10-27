using NaraEyes.Application.Contracts.Models.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class DeviceCommand : BaseCommand
    {
        public string CommandType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty; 
    }
}
