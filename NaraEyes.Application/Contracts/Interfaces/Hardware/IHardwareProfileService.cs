
using NaraEyes.Application.Contracts.Models.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Hardware
{
    public interface IHardwareProfileService
    {
        Task ProcessAsync(string deviceIp, HardwareProfilePayload payload, CancellationToken ct = default);
    }
}
