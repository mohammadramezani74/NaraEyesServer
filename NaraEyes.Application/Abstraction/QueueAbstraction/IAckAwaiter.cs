using NaraEyes.Application.Contracts.Models.Basic;

namespace NaraEyes.Application.Abstraction.QueueAbstraction
{
    public interface IAckAwaiter
    {
        Task<CommandAck> WaitForAckAsync(Guid commandId, TimeSpan timeout, CancellationToken ct);
        bool TrySetAck(Guid commandId, CommandAck ack);
    }
}
