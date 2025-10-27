using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.QueueImplemention
{
    internal class InBoxDeviceService : IInboxService
    {
        private readonly IApplicationUnitOfWork _uow;

        public InBoxDeviceService(IApplicationUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<InBoxDeviceMessage>> GetUnprocessedMessagesAsync(string deviceIp, CancellationToken ct)
        {
            return await _uow.InBoxDeviceMessages
             .Where(m => m.DeviceIp == deviceIp && !m.Processed)
             .OrderBy(m => m.CreateDate)
             .ToListAsync(ct);
        }

        public async Task MarkAsConsumedAsync(Guid messageId, CancellationToken ct)
        {
            var msg = await _uow.InBoxDeviceMessages
          .FirstOrDefaultAsync(m => m.Id == messageId, ct);

            if (msg != null)
            {
                msg.Processed = true;
                msg.ProcessedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
        }

        public async Task StoreBatchAsync(IReadOnlyList<InBoxDeviceMessage> messages, CancellationToken ct)
        {
            if (messages is null || messages.Count == 0) return;

            await _uow.InBoxDeviceMessages.AddRangeAsync(messages, ct);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task StoreMessageAsync(InBoxDeviceMessage message, CancellationToken ct)
        {
            await _uow.InBoxDeviceMessages.AddAsync(message, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
