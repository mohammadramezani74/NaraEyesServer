using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Contracts.Models.Basic;
using System.Collections.Concurrent;

namespace NaraEyes.Infrastructure.QueueImplemention
{
    public sealed class AckAwaiter : IAckAwaiter
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<CommandAck>> _waits = new();

        public Task<CommandAck> WaitForAckAsync(Guid commandId, TimeSpan timeout, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<CommandAck>(TaskCreationOptions.RunContinuationsAsynchronously);


            _waits[commandId] = tcs;

            _ = Task.Delay(timeout, ct).ContinueWith(_ =>
            {
                if (_waits.TryRemove(commandId, out var x))
                    x.TrySetException(new TimeoutException("Ack timed out"));
            }, TaskScheduler.Default);

            ct.Register(() =>
            {
                if (_waits.TryRemove(commandId, out var x))
                    x.TrySetCanceled(ct);
            });

            return tcs.Task;
        }

        public bool TrySetAck(Guid commandId, CommandAck ack)
        {
            if (_waits.TryRemove(commandId, out var tcs))
                return tcs.TrySetResult(ack);
            return false;
        }
  
    }
}
