using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Devices
{



    public sealed class DeviceSignalHub : IDeviceSignalHub
    {
        private sealed class AsyncAutoResetEvent
        {
            private readonly Queue<TaskCompletionSource<bool>> _waits = new();
            private bool _signaled;

            public Task WaitAsync(CancellationToken ct)
            {
                lock (_waits)
                {
                    if (_signaled) { _signaled = false; return Task.CompletedTask; }
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    CancellationTokenRegistration reg = default;
                    if (ct.CanBeCanceled)
                    {
                        reg = ct.Register(() => tcs.TrySetCanceled(ct));
                        _ = tcs.Task.ContinueWith(_ => reg.Dispose(), TaskScheduler.Default);
                    }
                    _waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
            public void Set()
            {
                TaskCompletionSource<bool> toRelease = null;
                lock (_waits)
                {
                    if (_waits.Count > 0) toRelease = _waits.Dequeue();
                    else _signaled = true; // ← حافظه سیگنال
                }
                toRelease?.TrySetResult(true);
            }
        }

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _map =
                new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        private SemaphoreSlim Get(string key) =>
            _map.GetOrAdd(key, _ => new SemaphoreSlim(0, int.MaxValue));


        public async Task<bool> WaitAsync(string deviceKey, TimeSpan timeout, CancellationToken ct)
        {
            var sem = Get(deviceKey);
            try
            {
                return await sem.WaitAsync(timeout, ct).ConfigureAwait(false);

            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public Task Pulse(string deviceKey)
        {
            var sem = Get(deviceKey);
            try { sem.Release(); }
            catch (SemaphoreFullException) { /* اگر زیاد Release شد، نادیده بگیر */ }
            return Task.CompletedTask;
        }
    }

}
