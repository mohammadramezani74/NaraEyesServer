using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Base
{
    public interface IHeartbeatThrottler
    {
        Task UpdateAsync(string deviceIp, CancellationToken ct);
    }
}
