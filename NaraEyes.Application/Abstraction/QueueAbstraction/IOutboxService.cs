using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Bulkoperations;
using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Abstraction.QueueAbstraction
{
    public interface IOutboxService
    {
        Task EnqueueCommandAsync(OutBoxDeviceMessage command, CancellationToken ct);
        Task<List<OutBoxDeviceMessage>> GetPendingCommandsAsync(string deviceIp, CancellationToken ct);
        Task MarkCommandAsProcessedAsync(Guid commandId, CancellationToken ct);
        Task MarkAutoJournalProccessor(string deviceIp, byte[]? file, CancellationToken ct);
        Task MarkCommandAsFailedAsync(Guid commandId, string? ip, CancellationToken ct);
        Task MarkCommandGroupProcessedAsync(SendGroupInstructionModel? pl, CancellationToken ct);
        Task MarkReportFailedSafeAsync(InBoxDeviceMessage msg, CancellationToken ct);
    }
}
