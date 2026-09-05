using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Alarms;
using NaraEyes.Application.Contracts.Models.Alarms;
using NaraEyes.Application.Hubs;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Services.Alarms
{
    public class AlarmService : IAlarmService
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IHubContext<DeviceHub> _hub;
        private readonly ILogger<AlarmService> _logger;

        /// <summary>
        /// اگر سازنده پنجره‌ای ندهد، این استفاده می‌شود.
        ///
        /// بدون سرکوب، یک دستگاه که هر سه دقیقه متریک می‌فرستد می‌تواند
        /// روزی ۴۸۰ آلارم یکسان بسازد. کاربر بعد از یک روز زنگوله را
        /// نادیده می‌گیرد و کل سامانه بی‌اثر می‌شود — این «خستگی از
        /// هشدار» است و مشکل شماره‌ی یک سامانه‌های مانیتورینگ.
        /// </summary>
        private static readonly TimeSpan DefaultSuppress = TimeSpan.FromHours(6);

        public AlarmService(
            IApplicationUnitOfWork uow,
            IHubContext<DeviceHub> hub,
            ILogger<AlarmService> logger)
        {
            _uow = uow;
            _hub = hub;
            _logger = logger;
        }

        // =============================================================
        //  ثبت
        // =============================================================

        public async Task<Guid?> RaiseAsync(
            Guid deviceId,
            DeviceModuleType module,
            EventSeverity severity,
            string code,
            string message,
            string? payloadJson = null,
            TimeSpan? suppressWindow = null,
            CancellationToken ct = default)
        {
            var window = suppressWindow ?? DefaultSuppress;
            var since = DateTime.Now - window;

            // ---- سرکوب تکراری ----
            //
            // فقط رویدادهای **تأییدنشده** مانع ثبت جدید می‌شوند. اگر
            // اپراتور قبلی را تأیید کرده باشد، یعنی دیده و رسیدگی کرده،
            // پس تکرار مشکل یک رویداد تازه است و باید دوباره دیده شود.
            bool duplicate = await _uow.DeviceEvents.AsNoTracking()
                .AnyAsync(x => x.DeviceId == deviceId
                            && x.Code == code
                            && !x.Acknowledged
                            && x.EventTime >= since, ct);

            if (duplicate) return null;

            var evt = DeviceEvent.Create(deviceId, module, severity, code, message, payloadJson);

            _uow.DeviceEvents.Add(evt);
            await _uow.SaveChangesAsync(ct);

            // ---- پخش ----
            //
            // بعد از ذخیره، نه قبلش. اگر ذخیره شکست بخورد نباید آلارمی
            // نمایش داده شود که در دیتابیس نیست و کاربر نتواند تأییدش کند.
            try
            {
                var info = await _uow.Devices.AsNoTracking()
                    .Where(d => d.Id == deviceId)
                    .Select(d => new
                    {
                        d.Ip,
                        BranchName = d.Branch != null ? d.Branch.Name : null
                    })
                    .FirstOrDefaultAsync(ct);

                await _hub.Clients.All.SendAsync("ReceiveAlarm", new AlarmNotification
                {
                    EventId = evt.Id,
                    DeviceId = deviceId,
                    DeviceIp = info?.Ip ?? "",
                    BranchName = info?.BranchName,
                    Severity = severity,
                    Code = code,
                    Message = message,
                    OccurredAt = evt.EventTime,
                }, ct);
            }
            catch (Exception ex)
            {
                // پخش نشدن نباید ثبت را باطل کند. آلارم در دیتابیس هست و
                // در رفرش بعدی دیده می‌شود.
                _logger.LogWarning(ex, "Alarm {Code} saved but broadcast failed", code);
            }

            return evt.Id;
        }

        // =============================================================
        //  خواندن
        // =============================================================

        public async Task<List<AlarmRow>> GetAsync(
            AlarmFilter filter, CancellationToken ct = default)
        {
            var q = _uow.DeviceEvents.AsNoTracking().Where(x => !x.Deleted);

            if (filter.FromDate.HasValue)
                q = q.Where(x => x.EventTime >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                q = q.Where(x => x.EventTime <= filter.ToDate.Value);

            if (filter.Severity.HasValue)
                q = q.Where(x => x.Severity == filter.Severity.Value);

            if (!string.IsNullOrWhiteSpace(filter.Code))
                q = q.Where(x => x.Code == filter.Code);

            if (filter.DeviceId.HasValue)
                q = q.Where(x => x.DeviceId == filter.DeviceId.Value);

            if (filter.BranchId.HasValue)
                q = q.Where(x => x.Device.BranchId == filter.BranchId.Value);

            if (filter.SupervisionId.HasValue)
                q = q.Where(x => x.Device.Branch != null
                              && x.Device.Branch.SupervisionId == filter.SupervisionId.Value);

            if (filter.Acknowledged.HasValue)
                q = q.Where(x => x.Acknowledged == filter.Acknowledged.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                q = q.Where(x => x.Message.Contains(s)
                              || x.Device.Ip.Contains(s)
                              || (x.Device.Branch != null && x.Device.Branch.Name.Contains(s)));
            }

            var take = filter.Take <= 0 ? 200 : Math.Min(filter.Take, 2000);

            var raw = await q
                .OrderByDescending(x => x.EventTime)
                .Take(take)
                .Select(x => new
                {
                    x.Id,
                    x.DeviceId,
                    DeviceIp = x.Device.Ip,
                    DeviceCode = x.Device.Code,
                    BranchName = x.Device.Branch != null ? x.Device.Branch.Name : null,
                    SupervisionName = x.Device.Branch != null && x.Device.Branch.Supervision != null
                        ? x.Device.Branch.Supervision.Name : null,
                    x.Severity,
                    x.Module,
                    x.Code,
                    x.Message,
                    x.PayloadJson,
                    x.EventTime,
                    x.Acknowledged,
                    x.AcknowledgedAt,
                    AckName = x.AcknowledgedBy != null ? x.AcknowledgedBy.UserName : null,
                })
                .ToListAsync(ct);

            return raw.Select(x => new AlarmRow
            {
                Id = x.Id,
                DeviceId = x.DeviceId,
                DeviceIp = x.DeviceIp,
                DeviceCode = x.DeviceCode,
                BranchName = x.BranchName,
                SupervisionName = x.SupervisionName,
                Severity = x.Severity,
                Module = x.Module,
                Code = x.Code,
                CodeFa = AlarmCodes.Fa(x.Code),
                Message = x.Message,
                PayloadJson = x.PayloadJson,
                EventTime = x.EventTime,
                Acknowledged = x.Acknowledged,
                AcknowledgedAt = x.AcknowledgedAt,
                AcknowledgedByName = x.AckName,
            }).ToList();
        }

        public async Task<AlarmCounts> GetCountsAsync(CancellationToken ct = default)
        {
            // رویدادهای Info آلارم نیستند — HW-BASELINE نباید badge را
            // بالا ببرد.
            var q = _uow.DeviceEvents.AsNoTracking()
                .Where(x => !x.Deleted
                         && !x.Acknowledged
                         && x.Severity != EventSeverity.Info);

            var grouped = await q
                .GroupBy(x => x.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var counts = new AlarmCounts();

            foreach (var g in grouped)
            {
                counts.Unacknowledged += g.Count;

                if (g.Severity == EventSeverity.Critical) counts.Critical = g.Count;
                else if (g.Severity == EventSeverity.Error) counts.Error = g.Count;
                else if (g.Severity == EventSeverity.Warning) counts.Warning = g.Count;
            }

            return counts;
        }

        // =============================================================
        //  تأیید
        // =============================================================

        public async Task<bool> AcknowledgeAsync(
            Guid eventId, Guid userId, CancellationToken ct = default)
        {
            var evt = await _uow.DeviceEvents.FirstOrDefaultAsync(x => x.Id == eventId, ct);
            if (evt is null) return false;

            evt.Acknowledge(userId);
            await _uow.SaveChangesAsync(ct);

            // به بقیه‌ی کاربران خبر بده تا badge آن‌ها هم به‌روز شود،
            // وگرنه دو اپراتور روی یک آلارم کار می‌کنند.
            try
            {
                await _hub.Clients.All.SendAsync("AlarmAcknowledged", eventId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ack broadcast failed for {EventId}", eventId);
            }

            return true;
        }

        public async Task<int> AcknowledgeManyAsync(
            IEnumerable<Guid> eventIds, Guid userId, CancellationToken ct = default)
        {
            var ids = eventIds.Distinct().ToList();
            if (ids.Count == 0) return 0;

            var items = await _uow.DeviceEvents
                .Where(x => ids.Contains(x.Id) && !x.Acknowledged)
                .ToListAsync(ct);

            foreach (var e in items) e.Acknowledge(userId);

            await _uow.SaveChangesAsync(ct);

            try
            {
                await _hub.Clients.All.SendAsync("AlarmsRefreshed", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk ack broadcast failed");
            }

            return items.Count;
        }
    }
}