using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;

namespace NaraEyes.Application.Abstraction.QueueAbstraction
{
    public interface IInboxService
    {
        Task StoreMessageAsync(InBoxDeviceMessage message, CancellationToken ct);
        Task<List<InBoxDeviceMessage>> GetUnprocessedMessagesAsync(string deviceIp, CancellationToken ct);
        Task MarkAsConsumedAsync(Guid messageId, CancellationToken ct);
        Task StoreBatchAsync(IReadOnlyList<InBoxDeviceMessage> messages, CancellationToken ct);
    }
}
