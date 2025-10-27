using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Devices
{
    public interface IDeviceSignalHub
    {
        Task<bool> WaitAsync(string deviceIp, TimeSpan timeout, CancellationToken ct);
        Task Pulse(string deviceIp);
    }
}
