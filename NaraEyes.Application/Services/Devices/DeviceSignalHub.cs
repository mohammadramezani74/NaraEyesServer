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
    

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _map =
                new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        private SemaphoreSlim Get(string key) =>
            _map.GetOrAdd(key, _ => new SemaphoreSlim(0, 1));


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
