using Microsoft.Extensions.DependencyInjection;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Base
{
    public sealed class HeartbeatThrottler : IHeartbeatThrottler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentDictionary<string, DateTime> _last = new();
        private readonly TimeSpan _minInterval;

        public HeartbeatThrottler(IServiceScopeFactory scopeFactory, TimeSpan? minInterval = null)
        {
            _scopeFactory = scopeFactory;
            _minInterval = minInterval ?? TimeSpan.FromMinutes(2);
        }

        public async Task UpdateAsync(string deviceIp, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var last = _last.GetOrAdd(deviceIp, _ => DateTime.MinValue);
            if (now - last < _minInterval) return;

            // تضمین اینکه فقط یک نویسنده Heartbeat اجرا شود
            if (!_last.TryUpdate(deviceIp, now, last)) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var deviceService = scope.ServiceProvider.GetRequiredService<IDeviceService>();
                await deviceService.UpdateHeartbeatAsync(deviceIp, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch
            {
                // TODO: log error
            }
        }
    }
}
