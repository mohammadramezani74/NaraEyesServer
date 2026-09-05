using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Common.Excel;
using NaraEyes.Application.Contracts.Interfaces.Reports;
using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Services.Reports
{
    public class DeviceAvailabilityReportService : IDeviceAvailabilityReportService
    {
        private readonly IApplicationUnitOfWork _uow;

        public DeviceAvailabilityReportService(IApplicationUnitOfWork uow) => _uow = uow;

        /// <summary>یک بازه‌ی زمانی ساده — برای برش دادن و کم کردن</summary>
        private readonly record struct Window(DateTime Start, DateTime End)
        {
            public double Seconds => Math.Max(0, (End - Start).TotalSeconds);
        }

        // =============================================================
        //  خلاصه
        // =============================================================

        public async Task<DeviceAvailabilityResult> GetSummaryAsync(
            DeviceAvailabilityFilter filter, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var from = filter.FromDate;
            var to = filter.ToDate > now ? now : filter.ToDate;

            if (to <= from) return new DeviceAvailabilityResult();

            var minOutage = Math.Max(0, filter.MinOutageMinutes) * 60;

            // ---- بازه‌هایی که با پنجره‌ی گزارش همپوشانی دارند ----
            //
            // ⚠️ شرط عمداً «StartedAt داخل بازه» نیست.
            // بازه‌ای که سه هفته قبل باز شده و هنوز باز است، در کل پنجره
            // اثر دارد ولی StartedAt‌اش بیرون است. اگر مثل گزارش خرابی
            // قطعات فقط StartedAt را فیلتر کنیم، آن دستگاه از گزارش حذف
            // می‌شود — و دقیقاً دستگاهی که یک ماه خراب بوده مهم‌ترین ردیف
            // گزارش است.
            var query = _uow.DeviceStateLogs.AsNoTracking()
                .Where(x => x.StartedAt <= to && (x.EndedAt == null || x.EndedAt >= from));

            query = ApplyDeviceFilters(query, filter);

            var raw = await query
                .Select(x => new
                {
                    x.DeviceId,
                    DeviceIp = x.Device.Ip,
                    DeviceName = x.Device.Model,
                    DeviceCode = x.Device.Code,
                    BranchName = x.Device.Branch != null ? x.Device.Branch.Name : null,
                    SupervisionName = x.Device.Branch != null && x.Device.Branch.Supervision != null
                        ? x.Device.Branch.Supervision.Name : null,
                    Vendor = x.Device.Vendor,
                    x.State,
                    x.StartedAt,
                    x.EndedAt,
                })
                .ToListAsync(ct);

            // ---- بازه‌های قطعی سرور ----
            var outages = filter.ExcludeServerOutages
                ? await GetServerOutagesAsync(from, to, ct)
                : new List<Window>();

            long excludedSeconds = (long)outages.Sum(o => o.Seconds);

            // ---- تجمیع به‌ازای دستگاه ----
            var rows = new List<DeviceAvailabilityRow>();

            foreach (var g in raw.GroupBy(x => x.DeviceId))
            {
                var first = g.First();

                var row = new DeviceAvailabilityRow
                {
                    DeviceId = g.Key,
                    DeviceIp = first.DeviceIp,
                    DeviceName = first.DeviceName,
                    DeviceCode = first.DeviceCode,
                    BranchName = first.BranchName,
                    SupervisionName = first.SupervisionName,
                    Vendor = first.Vendor,
                    HasData = true,
                };

                foreach (var iv in g)
                {
                    // برش به پنجره‌ی گزارش
                    var s = iv.StartedAt < from ? from : iv.StartedAt;
                    var e = iv.EndedAt ?? now;
                    if (e > to) e = to;
                    if (e <= s) continue;

                    // کم کردن مدتی که سرور خاموش بوده
                    long secs = (long)EffectiveSeconds(new Window(s, e), outages);
                    if (secs <= 0) continue;

                    if (iv.State == AvailabilityState.Available) row.AvailableSeconds += secs;
                    else if (iv.State == AvailabilityState.OutOfService) row.OutOfServiceSeconds += secs;
                    else if (iv.State == AvailabilityState.Error) row.ErrorSeconds += secs;
                    else if (iv.State == AvailabilityState.Disconnected) row.DisconnectedSeconds += secs;
                    else row.UnknownSeconds += secs;

                    row.ObservedSeconds += secs;

                    // شمارش دفعات خروج — فقط بازه‌هایی که از آستانه بلندترند
                    if (iv.State != AvailabilityState.Available
                        && iv.State != AvailabilityState.Unknown
                        && secs >= minOutage)
                    {
                        row.OutageCount++;
                        if (secs > row.LongestOutageSeconds)
                            row.LongestOutageSeconds = (int)secs;

                        if (row.LastOutageAt is null || iv.StartedAt > row.LastOutageAt)
                            row.LastOutageAt = iv.StartedAt;
                    }
                }

                // وضعیت فعلی = بازه‌ای که هنوز باز است
                var open = g.Where(x => x.EndedAt == null)
                            .OrderByDescending(x => x.StartedAt)
                            .FirstOrDefault();

                if (open is not null)
                {
                    row.CurrentState = open.State;
                    row.CurrentStateSince = open.StartedAt;
                }
                else
                {
                    var last = g.OrderByDescending(x => x.EndedAt).First();
                    row.CurrentState = AvailabilityState.Unknown;
                    row.CurrentStateSince = last.EndedAt;
                }

                rows.Add(row);
            }

            // ---- دستگاه‌هایی که در این بازه هیچ داده‌ای ندارند ----
            //
            // این‌ها را نمی‌شود ساکت حذف کرد. دستگاهی که اصلاً گزارش نداده
            // ممکن است تازه نصب شده باشد، یا ممکن است ایجنتش سه هفته است
            // بالا نیامده — و حالت دوم دقیقاً چیزی است که باید دیده شود.
            var seen = rows.Select(r => r.DeviceId).ToHashSet();
            var missing = await GetDevicesWithoutDataAsync(filter, seen, ct);
            rows.AddRange(missing);

            if (filter.OnlyProblematic)
                rows = rows.Where(r => r.CurrentState != AvailabilityState.Available).ToList();

            if (filter.State.HasValue)
            {
                var st = filter.State.Value;
                rows = rows.Where(r => StateSeconds(r, st) > 0).ToList();
            }

            rows = rows.OrderBy(r => r.HasData ? 0 : 1)
                       .ThenBy(r => r.AvailabilityPercent)
                       .ToList();

            // ---- شاخص‌های کلی ----
            long totalAvail = rows.Sum(r => r.AvailableSeconds);
            long totalObserved = rows.Sum(r => r.ObservedSeconds - r.UnknownSeconds);

            var byReason = new List<AvailabilityChartPoint>
            {
                new() { LabelFa = "خارج از سرویس", Seconds = rows.Sum(r => r.OutOfServiceSeconds) },
                new() { LabelFa = "خطا",           Seconds = rows.Sum(r => r.ErrorSeconds) },
                new() { LabelFa = "قطع ارتباط",    Seconds = rows.Sum(r => r.DisconnectedSeconds) },
            };

            return new DeviceAvailabilityResult
            {
                Rows = rows,

                // میانگین وزنی بر اساس مدت، نه میانگین درصدها. اگر میانگین
                // ساده‌ی درصدها را بگیریم، دستگاهی که فقط یک روز رصد شده
                // همان وزنی را دارد که دستگاهی با یک ماه داده — و یک دستگاه
                // تازه‌نصب می‌تواند عدد کل ناوگان را جابه‌جا کند.
                FleetAvailabilityPercent = totalObserved <= 0
                    ? 0
                    : Math.Round(totalAvail * 100.0 / totalObserved, 2),

                DeviceCount = rows.Count,
                DevicesWithoutData = rows.Count(r => !r.HasData),
                CurrentlyDown = rows.Count(r => r.HasData
                                             && r.CurrentState != AvailabilityState.Available
                                             && r.CurrentState != AvailabilityState.Unknown),
                TotalOutages = rows.Sum(r => r.OutageCount),

                TotalAvailableSeconds = totalAvail,
                TotalOutOfServiceSeconds = byReason[0].Seconds,
                TotalErrorSeconds = byReason[1].Seconds,
                TotalDisconnectedSeconds = byReason[2].Seconds,

                ExcludedServerOutageSeconds = excludedSeconds,
                ByReason = byReason.Where(p => p.Seconds > 0).ToList(),
            };
        }

        // =============================================================
        //  جزئیات یک دستگاه
        // =============================================================

        public async Task<List<DeviceStateDetailRow>> GetDetailsAsync(
            Guid deviceId, DeviceAvailabilityFilter filter, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var from = filter.FromDate;
            var to = filter.ToDate > now ? now : filter.ToDate;

            var raw = await _uow.DeviceStateLogs.AsNoTracking()
                .Where(x => x.DeviceId == deviceId
                         && x.StartedAt <= to
                         && (x.EndedAt == null || x.EndedAt >= from))
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new
                {
                    x.Id,
                    x.State,
                    x.StartMode,
                    x.CurrentMode,
                    x.StartedAt,
                    x.EndedAt,
                    x.DurationSeconds,
                    x.TransitionCount,
                })
                .ToListAsync(ct);

            return raw.Select(x => new DeviceStateDetailRow
            {
                Id = x.Id,
                State = x.State,
                StateFa = AvailabilityMapping.Fa(x.State),
                StartMode = x.StartMode,
                CurrentMode = x.CurrentMode,
                ModeFa = AvailabilityMapping.ModeFa(x.CurrentMode),
                StartedAt = x.StartedAt,
                EndedAt = x.EndedAt,
                DurationSeconds = x.DurationSeconds
                                  ?? Math.Max(0, (int)(now - x.StartedAt).TotalSeconds),
                TransitionCount = x.TransitionCount,
            }).ToList();
        }

        // =============================================================
        //  خروجی اکسل
        // =============================================================

        public async Task<byte[]> ExportExcelAsync(
            DeviceAvailabilityFilter filter, CancellationToken ct = default)
        {
            var result = await GetSummaryAsync(filter, ct);

            using var xl = new ReportExcelBuilder("آماده‌به‌کاری");

            xl.AddTitle(
                "گزارش آماده‌به‌کاری دستگاه‌ها",
                $"از {ReportExcelBuilder.ToJalali(filter.FromDate)} " +
                $"تا {ReportExcelBuilder.ToJalali(filter.ToDate)}");

            xl.AddSummary(new[]
            {
                ("آماده‌به‌کاری ناوگان", result.FleetAvailabilityPercent.ToString("0.00") + "٪"),
                ("تعداد دستگاه",         result.DeviceCount.ToString("#,##0")),
                ("بدون داده",            result.DevicesWithoutData.ToString("#,##0")),
                ("هم‌اکنون خارج از سرویس", result.CurrentlyDown.ToString("#,##0")),
                ("کل دفعات خروج",        result.TotalOutages.ToString("#,##0")),
                ("خارج از سرویس",        ReportExcelBuilder.FormatDuration(result.TotalOutOfServiceSeconds)),
                ("خطا",                  ReportExcelBuilder.FormatDuration(result.TotalErrorSeconds)),
                ("قطع ارتباط",           ReportExcelBuilder.FormatDuration(result.TotalDisconnectedSeconds)),
                ("قطعی سرور (کسر شد)",   ReportExcelBuilder.FormatDuration(result.ExcludedServerOutageSeconds)),
            });

            xl.AddHeader(
                "ردیف", "کد دستگاه", "آی‌پی", "نام دستگاه", "شعبه", "سرپرستی", "سازنده",
                "آماده‌به‌کاری ٪", "مدت آماده", "خارج از سرویس", "خطا", "قطع ارتباط",
                "دفعات خروج", "طولانی‌ترین خروج", "وضعیت فعلی");

            int i = 1;
            foreach (var r in result.Rows)
            {
                RowTone? tone = null;
                if (!r.HasData) tone = RowTone.Warning;
                else if (r.AvailabilityPercent < 90) tone = RowTone.Error;
                else if (r.AvailabilityPercent < 98) tone = RowTone.Warning;

                xl.AddRow(tone,
                    i++,
                    r.DeviceCode,
                    r.DeviceIp,
                    r.DeviceName,
                    r.BranchName,
                    r.SupervisionName,
                    r.VendorFa,
                    r.HasData ? r.AvailabilityPercent : (object?)"—",
                    ReportExcelBuilder.FormatDuration(r.AvailableSeconds),
                    ReportExcelBuilder.FormatDuration(r.OutOfServiceSeconds),
                    ReportExcelBuilder.FormatDuration(r.ErrorSeconds),
                    ReportExcelBuilder.FormatDuration(r.DisconnectedSeconds),
                    r.OutageCount,
                    ReportExcelBuilder.FormatDuration(r.LongestOutageSeconds),
                    r.HasData ? AvailabilityMapping.Fa(r.CurrentState) : "بدون داده");
            }

            xl.Finish();
            return xl.ToBytes();
        }

        // =============================================================
        //  کمکی
        // =============================================================

        /// <summary>
        /// مدت مؤثر یک بازه پس از کم کردن بازه‌های قطعی سرور.
        ///
        /// قطعی‌ها مرتب و بدون همپوشانی‌اند، پس یک پیمایش ساده کافی است.
        /// </summary>
        private static double EffectiveSeconds(Window w, List<Window> outages)
        {
            double total = w.Seconds;
            if (total <= 0 || outages.Count == 0) return total;

            foreach (var o in outages)
            {
                if (o.End <= w.Start) continue;
                if (o.Start >= w.End) break;

                var s = o.Start > w.Start ? o.Start : w.Start;
                var e = o.End < w.End ? o.End : w.End;

                if (e > s) total -= (e - s).TotalSeconds;
            }

            return total < 0 ? 0 : total;
        }

        /// <summary>
        /// فاصله‌های بین بازه‌های اجرای سرور — یعنی مدتی که سرور نمی‌دانسته
        /// چه خبر است.
        ///
        /// اگر جدول خالی باشد (هنوز فعال نشده) لیست خالی برمی‌گردد و
        /// محاسبه دقیقاً مثل قبل انجام می‌شود.
        /// </summary>
        private async Task<List<Window>> GetServerOutagesAsync(
            DateTime from, DateTime to, CancellationToken ct)
        {
            var runs = await _uow.ServerUptimeLogs.AsNoTracking()
                .Where(x => x.LastAliveAt >= from.AddDays(-1) && x.StartedAt <= to)
                .OrderBy(x => x.StartedAt)
                .Select(x => new { x.StartedAt, x.LastAliveAt })
                .ToListAsync(ct);

            var result = new List<Window>();
            if (runs.Count == 0) return result;

            for (int i = 1; i < runs.Count; i++)
            {
                var gapStart = runs[i - 1].LastAliveAt;
                var gapEnd = runs[i].StartedAt;

                if (gapEnd <= gapStart) continue;

                // فاصله‌ی کمتر از دو دقیقه یعنی ضربان معمولی، نه قطعی
                if ((gapEnd - gapStart).TotalMinutes < 2) continue;

                var s = gapStart < from ? from : gapStart;
                var e = gapEnd > to ? to : gapEnd;
                if (e > s) result.Add(new Window(s, e));
            }

            return result;
        }

        private async Task<List<DeviceAvailabilityRow>> GetDevicesWithoutDataAsync(
            DeviceAvailabilityFilter filter, HashSet<Guid> seen, CancellationToken ct)
        {
            var q = _uow.Devices.AsNoTracking().Where(d => !seen.Contains(d.Id));

            if (filter.BranchId.HasValue)
                q = q.Where(d => d.BranchId == filter.BranchId.Value);

            if (filter.SupervisionId.HasValue)
                q = q.Where(d => d.Branch != null
                              && d.Branch.SupervisionId == filter.SupervisionId.Value);

            if (filter.Vendor.HasValue)
                q = q.Where(d => d.Vendor == filter.Vendor.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                q = q.Where(d => d.Ip.Contains(s)
                              || (d.Model != null && d.Model.Contains(s))
                              || (d.SerialNo != null && d.SerialNo.Contains(s)));
            }

            return await q.Select(d => new DeviceAvailabilityRow
            {
                DeviceId = d.Id,
                DeviceIp = d.Ip,
                DeviceName = d.Model,
                DeviceCode = d.Code,
                BranchName = d.Branch != null ? d.Branch.Name : null,
                SupervisionName = d.Branch != null && d.Branch.Supervision != null
                    ? d.Branch.Supervision.Name : null,
                Vendor = d.Vendor,
                HasData = false,
                CurrentState = AvailabilityState.Unknown,
            }).ToListAsync(ct);
        }

        private static IQueryable<Domain.Entities.Devices.DeviceStateLog> ApplyDeviceFilters(
            IQueryable<Domain.Entities.Devices.DeviceStateLog> q,
            DeviceAvailabilityFilter f)
        {
            if (f.BranchId.HasValue)
                q = q.Where(x => x.Device.BranchId == f.BranchId.Value);

            if (f.SupervisionId.HasValue)
                q = q.Where(x => x.Device.Branch != null
                              && x.Device.Branch.SupervisionId == f.SupervisionId.Value);

            if (f.Vendor.HasValue)
                q = q.Where(x => x.Device.Vendor == f.Vendor.Value);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(x => x.Device.Ip.Contains(s)
                              || (x.Device.Model != null && x.Device.Model.Contains(s))
                              || (x.Device.SerialNo != null && x.Device.SerialNo.Contains(s)));
            }

            return q;
        }

        private static long StateSeconds(DeviceAvailabilityRow r, AvailabilityState s)
        {
            if (s == AvailabilityState.Available) return r.AvailableSeconds;
            if (s == AvailabilityState.OutOfService) return r.OutOfServiceSeconds;
            if (s == AvailabilityState.Error) return r.ErrorSeconds;
            if (s == AvailabilityState.Disconnected) return r.DisconnectedSeconds;
            return r.UnknownSeconds;
        }
    }
}