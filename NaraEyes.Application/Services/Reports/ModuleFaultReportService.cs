using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Common.Excel;
using NaraEyes.Application.Contracts.Interfaces.Reports;
using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Services.Reports
{
    public class ModuleFaultReportService : IModuleFaultReportService
    {
        private readonly IApplicationUnitOfWork _uow;

        public ModuleFaultReportService(IApplicationUnitOfWork uow) => _uow = uow;

        // =============================================================
        //  خلاصه
        // =============================================================

        public async Task<ModuleFaultReportResult> GetSummaryAsync(
            ModuleFaultFilter filter, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var minSeconds = Math.Max(0, filter.MinDurationMinutes) * 60;

            var query = BuildBaseQuery(filter);

            // ابتدا داده‌ی خام را می‌آوریم چون محاسبه‌ی مدت برای بازه‌های
            // باز به «الان» وابسته است و در SQL به‌سختی قابل بیان است.
            var raw = await query
                .Select(f => new
                {
                    f.Id,
                    f.DeviceId,
                    DeviceIp = f.Device.Ip,
                    DeviceName = f.Device.Model,
                    DeviceCode = f.Device.Code,
                    BranchName = f.Device.Branch != null ? f.Device.Branch.Name : null,
                    SupervisionName = f.Device.Branch != null && f.Device.Branch.Supervision != null
                        ? f.Device.Branch.Supervision.Name : null,
                    Vendor = f.Device.Vendor,
                    f.Module,
                    f.StartedAt,
                    f.ResolvedAt,
                    f.DurationSeconds,
                })
                .ToListAsync(ct);

            // محاسبه‌ی مدت — بازه‌های باز تا لحظه‌ی گزارش
            var withDuration = raw
                .Select(x => new
                {
                    x.DeviceId,
                    x.DeviceIp,
                    x.DeviceName,
                    x.DeviceCode,
                    x.BranchName,
                    x.SupervisionName,
                    x.Vendor,
                    x.Module,
                    x.StartedAt,
                    x.ResolvedAt,
                    Duration = x.DurationSeconds
                               ?? Math.Max(0, (int)(now - x.StartedAt).TotalSeconds),
                    IsOpen = x.ResolvedAt == null,
                })
                .Where(x => x.Duration >= minSeconds)
                .ToList();

            if (filter.OnlyOpen)
                withDuration = withDuration.Where(x => x.IsOpen).ToList();

            var rows = withDuration
                .GroupBy(x => new { x.DeviceId, x.Module })
                .Select(g =>
                {
                    var first = g.First();
                    return new ModuleFaultSummaryRow
                    {
                        DeviceId = g.Key.DeviceId,
                        DeviceIp = first.DeviceIp,
                        DeviceName = first.DeviceName,
                        DeviceCode = first.DeviceCode,
                        BranchName = first.BranchName,
                        SupervisionName = first.SupervisionName,
                        Vendor = first.Vendor,
                        Module = g.Key.Module,
                        ModuleFa = ModuleFa(g.Key.Module),

                        FaultCount = g.Count(),
                        TotalDownSeconds = g.Sum(x => (long)x.Duration),
                        MaxDownSeconds = g.Max(x => x.Duration),
                        LastFaultAt = g.Max(x => x.StartedAt),
                        HasOpenFault = g.Any(x => x.IsOpen),
                    };
                })
                .OrderByDescending(r => r.TotalDownSeconds)
                .ToList();

            // نمودار — تجمیع بر اساس ماژول
            var byModule = withDuration
                .GroupBy(x => x.Module)
                .Select(g => new ModuleFaultChartPoint
                {
                    ModuleFa = ModuleFa(g.Key),
                    Count = g.Count(),
                    DownSeconds = g.Sum(x => (long)x.Duration),
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return new ModuleFaultReportResult
            {
                Rows = rows,
                TotalFaults = withDuration.Count,
                TotalDownSeconds = withDuration.Sum(x => (long)x.Duration),
                OpenFaults = withDuration.Count(x => x.IsOpen),
                AffectedDevices = withDuration.Select(x => x.DeviceId).Distinct().Count(),
                WorstModuleFa = byModule.FirstOrDefault()?.ModuleFa,
                ByModule = byModule,
            };
        }

        // =============================================================
        //  جزئیات یک دستگاه × ماژول
        // =============================================================

        public async Task<List<ModuleFaultDetailRow>> GetDetailsAsync(
            Guid deviceId, DeviceModuleType module,
            ModuleFaultFilter filter, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var minSeconds = Math.Max(0, filter.MinDurationMinutes) * 60;

            var raw = await _uow.ModuleFaultLogs.AsNoTracking()
                .Where(f => f.DeviceId == deviceId
                         && f.Module == module
                         && f.StartedAt >= filter.FromDate
                         && f.StartedAt <= filter.ToDate)
                .OrderByDescending(f => f.StartedAt)
                .Select(f => new
                {
                    f.Id,
                    DeviceIp = f.Device.Ip,
                    BranchName = f.Device.Branch != null ? f.Device.Branch.Name : null,
                    f.Module,
                    f.StartStatus,
                    f.CurrentStatus,
                    f.Detail,
                    f.StartedAt,
                    f.ResolvedAt,
                    f.DurationSeconds,
                    f.TransitionCount,
                })
                .ToListAsync(ct);

            return raw
                .Select(x => new ModuleFaultDetailRow
                {
                    Id = x.Id,
                    DeviceIp = x.DeviceIp,
                    BranchName = x.BranchName,
                    Module = x.Module,
                    ModuleFa = ModuleFa(x.Module),
                    StartStatus = x.StartStatus,
                    CurrentStatus = x.CurrentStatus,
                    StatusFa = StatusFa(x.CurrentStatus),
                    Detail = x.Detail,
                    StartedAt = x.StartedAt,
                    ResolvedAt = x.ResolvedAt,
                    DurationSeconds = x.DurationSeconds
                                      ?? Math.Max(0, (int)(now - x.StartedAt).TotalSeconds),
                    TransitionCount = x.TransitionCount,
                })
                .Where(x => x.DurationSeconds >= minSeconds)
                .ToList();
        }

        // =============================================================
        //  خروجی اکسل
        // =============================================================

        public async Task<byte[]> ExportExcelAsync(
            ModuleFaultFilter filter, CancellationToken ct = default)
        {
            var result = await GetSummaryAsync(filter, ct);

            using var xl = new ReportExcelBuilder("خرابی قطعات");

            xl.AddTitle(
                "گزارش خرابی قطعات",
                $"از {ReportExcelBuilder.ToJalali(filter.FromDate)} " +
                $"تا {ReportExcelBuilder.ToJalali(filter.ToDate)}" +
                $"  |  حداقل مدت: {filter.MinDurationMinutes} دقیقه");

            xl.AddSummary(new[]
            {
                ("کل خرابی‌ها",      result.TotalFaults.ToString("#,##0")),
                ("مجموع زمان خرابی", ReportExcelBuilder.FormatDuration(result.TotalDownSeconds)),
                ("خرابی فعلی",       result.OpenFaults.ToString("#,##0")),
                ("دستگاه درگیر",     result.AffectedDevices.ToString("#,##0")),
                ("پرخراب‌ترین ماژول", result.WorstModuleFa ?? "—"),
            });

            xl.AddHeader(
                "ردیف", "کد دستگاه", "آی‌پی", "نام دستگاه", "شعبه", "سرپرستی",
                "سازنده", "ماژول", "تعداد خرابی", "مجموع زمان",
                "میانگین (دقیقه)", "بیشترین", "آخرین خرابی", "وضعیت");

            int i = 1;
            foreach (var r in result.Rows)
            {
                // سطرهایی که هنوز خراب‌اند قرمز، پرتکرارها کهربایی
                var tone = r.HasOpenFault ? RowTone.Error
                         : r.FaultCount >= 5 ? RowTone.Warning
                         : (RowTone?)null;

                xl.AddRow(tone,
                    i++,
                    r.DeviceCode,
                    r.DeviceIp,
                    r.DeviceName,
                    r.BranchName,
                    r.SupervisionName,
                    r.VendorFa,
                    r.ModuleFa,
                    r.FaultCount,
                    ReportExcelBuilder.FormatDuration(r.TotalDownSeconds),
                    r.AvgDownMinutes,
                    ReportExcelBuilder.FormatDuration(r.MaxDownSeconds),
                    ReportExcelBuilder.ToJalali(r.LastFaultAt, withTime: true),
                    r.HasOpenFault ? "در حال خرابی" : "برطرف شده");
            }

            xl.Finish();
            return xl.ToBytes();
        }

        // =============================================================
        //  کمکی
        // =============================================================

        private IQueryable<Domain.Entities.Devices.ModuleFaultLog> BuildBaseQuery(
            ModuleFaultFilter f)
        {
            var q = _uow.ModuleFaultLogs.AsNoTracking()
                .Where(x => x.StartedAt >= f.FromDate && x.StartedAt <= f.ToDate);

            if (f.Module.HasValue)
                q = q.Where(x => x.Module == f.Module.Value);

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

        public static string ModuleFa(DeviceModuleType t) => t switch
        {
            DeviceModuleType.Cdm => "دیسپنسر",
            DeviceModuleType.Idc => "کارت‌خوان",
            DeviceModuleType.Ptr => "چاپگر رسید",
            DeviceModuleType.Pin => "پین‌پد",
            DeviceModuleType.Siu => "حسگرها",
            DeviceModuleType.Camera => "دوربین",
            DeviceModuleType.Ups => "برق اضطراری",
            DeviceModuleType.Network => "شبکه",
            DeviceModuleType.Journal => "ژورنال",
            _ => t.ToString(),
        };

        public static string StatusFa(HealthStatus s) => s switch
        {
            HealthStatus.Online => "آنلاین",
            HealthStatus.Offline => "آفلاین",
            HealthStatus.PowerOff => "بدون برق",
            HealthStatus.DeviceNotFound => "دستگاه یافت نشد",
            HealthStatus.HardwareError => "خطای سخت‌افزاری",
            HealthStatus.UserError => "خطای کاربری",
            HealthStatus.Busy => "مشغول",
            HealthStatus.FraudAttempt => "تلاش برای تقلب",
            HealthStatus.PotentialFraud => "احتمال تقلب",
            _ => "نامشخص",
        };
    }
}