using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Base
{
    public sealed class InBoxBatchWriter : BackgroundService, IInBoxBatchWriter
    {
        private readonly Channel<InBoxDeviceMessage> _ch;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _maxBatch = 200;
        private readonly TimeSpan _flushInterval = TimeSpan.FromMilliseconds(300);

        public InBoxBatchWriter(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _ch = Channel.CreateBounded<InBoxDeviceMessage>(
                new BoundedChannelOptions(5000)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
        }

        public void Enqueue(string deviceIp, IEnumerable<InBoxDeviceMessage> messages)
        {
            foreach (var m in messages)
            {
                m.DeviceIp = deviceIp;
                _ch.Writer.TryWrite(m); // در فشار زیاد، قدیمی‌ترین Drop می‌شود
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var buffer = new List<InBoxDeviceMessage>(_maxBatch);
            var sw = Stopwatch.StartNew();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    while (buffer.Count < _maxBatch &&
                           sw.Elapsed < _flushInterval &&
                           await _ch.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                    {
                        while (buffer.Count < _maxBatch && _ch.Reader.TryRead(out var item))
                            buffer.Add(item);
                    }

                    if (buffer.Count > 0)
                    {
                        // 🔑 هر Flush → یک Scope جدید (DbContext کوتاه‌عمر و ایمن)
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var inbox = scope.ServiceProvider.GetRequiredService<IInboxService>();
                            await inbox.StoreBatchAsync(buffer, stoppingToken).ConfigureAwait(false);
                        }

                        buffer.Clear();
                        sw.Restart();
                    }
                    else
                    {
                        await Task.Delay(_flushInterval, stoppingToken).ConfigureAwait(false);
                        sw.Restart();
                    }
                }
                catch (OperationCanceledException) { /* shutdown */ }
                catch
                {
                    // log error
                    await Task.Delay(250, stoppingToken);
                }
            }
        }
    }
}
