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

        public async Task<byte[]> WaitForBytesAsync(Guid commandId, TimeSpan timeout, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waits[commandId] = tcs;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            using var reg = timeoutCts.Token.Register(() =>
            {
                if (_waits.TryRemove(commandId, out var x))
                {
                    if (ct.IsCancellationRequested) x.TrySetCanceled(ct);
                    else x.TrySetException(new TimeoutException("Command timed out"));
                }
            });

            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _waits.TryRemove(commandId, out _);   // تضمین پاکسازی
            }
        }

        public bool TrySetResult(Guid commandId, byte[] data)
        {
            if (_waits.TryRemove(commandId, out var tcs))
                return tcs.TrySetResult(data);
            return false;
        }


    }
}
