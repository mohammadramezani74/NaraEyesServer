using NaraEyes.Application.Abstraction.QueueAbstraction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.QueueImplemention
{
    public sealed class CommandAwaiter : ICommandAwaiter
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> _waits = new();

        public Task<byte[]> WaitForBytesAsync(Guid commandId, TimeSpan timeout, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waits[commandId] = tcs;

            _ = Task.Delay(timeout, ct).ContinueWith(_ =>
            {
                if (_waits.TryRemove(commandId, out var x))
                    x.TrySetException(new TimeoutException("Screenshot timed out"));
            });

            ct.Register(() =>
            {
                if (_waits.TryRemove(commandId, out var x))
                    x.TrySetCanceled(ct);
            });

            return tcs.Task;
        }

        public bool TrySetResult(Guid commandId, byte[] data)
        {
            if (_waits.TryRemove(commandId, out var tcs))
                return tcs.TrySetResult(data);
            return false;
        }


    }
}
