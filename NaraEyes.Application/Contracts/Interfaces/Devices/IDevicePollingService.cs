using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Devices
{
    public interface IDevicePollingService
    {
        Task<PollResponse> PollAsync(
    string deviceIp,
    List<InBoxDeviceMessage>? reports,
    CancellationToken ct);
    }
}
