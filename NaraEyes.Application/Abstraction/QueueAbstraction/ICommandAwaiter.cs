using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.QueueAbstraction
{
    public interface ICommandAwaiter
    {
        Task<byte[]> WaitForBytesAsync(Guid commandId, TimeSpan timeout, CancellationToken ct);
        bool TrySetResult(Guid commandId, byte[] data);
     
    }
}
