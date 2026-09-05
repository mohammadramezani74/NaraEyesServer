using NaraEyes.Application.Contracts.Models.Alarms;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Interfaces.Alarms
{
    public interface IAlarmService
    {
        /// <summary>
        /// ثبت رویداد و پخش آنی آن.
        ///
        /// اگر رویداد مشابهی در پنجره‌ی سرکوب باز و تأییدنشده باشد،
        /// چیزی ثبت نمی‌شود و null برمی‌گردد.
        /// </summary>
        Task<Guid?> RaiseAsync(
            Guid deviceId,
            DeviceModuleType module,
            EventSeverity severity,
            string code,
            string message,
            string? payloadJson = null,
            TimeSpan? suppressWindow = null,
            CancellationToken ct = default);

        Task<List<AlarmRow>> GetAsync(AlarmFilter filter, CancellationToken ct = default);

        Task<AlarmCounts> GetCountsAsync(CancellationToken ct = default);

        Task<bool> AcknowledgeAsync(Guid eventId, Guid userId, CancellationToken ct = default);

        Task<int> AcknowledgeManyAsync(
            IEnumerable<Guid> eventIds, Guid userId, CancellationToken ct = default);
    }
}