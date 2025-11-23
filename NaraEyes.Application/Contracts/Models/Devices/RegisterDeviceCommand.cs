using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public sealed record RegisterDeviceCommand
        (int TerminalCode, string ip, string model, string? serialNo, string? agentVersion, DeviceMode mode);


}
