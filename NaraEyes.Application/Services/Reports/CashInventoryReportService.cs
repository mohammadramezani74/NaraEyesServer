using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Common.Excel;
using NaraEyes.Application.Contracts.Interfaces.Reports;
using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Services.Reports
{
    public class CashInventoryReportService : ICashInventoryReportService
    {
        private readonly IApplicationUnitOfWork _uow;

        public CashInventoryReportService(IApplicationUnitOfWork uow) => _uow = uow;

        // =============================================================
        //  گزارش اصلی
        // =============================================================

        public async Task<CashInventoryResult> GetInventoryAsync(
            CashInventoryFilter filter, CancellationToken ct = default)
        {
            // ---- خواندن داده‌ی خام ----
            // محاسبه در حافظه انجام می‌شود چون CurrentCount از نوع string
            // است و SQL نمی‌تواند مطمئن تبدیلش کند.
            var raw = await BuildQuery(filter)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Currency,
                    c.Denomination,
                    c.CurrentCount,
                    c.TotalCount,
                    c.Type,
                    c.Status,

                    c.DeviceId,
                    DeviceIp = c.Device.Ip,
                    DeviceName = c.Device.Model,
                    DeviceCode = c.Device.Code,
                    Vendor = c.Device.Vendor,

                    BranchId = c.Device.BranchId,
                    BranchName = c.Device.Branch != null ? c.Device.Branch.Name : null,
                    SupervisionId = c.Device.Branch != null ? (Guid?)c.Device.Branch.SupervisionId : null,
                    SupervisionName = c.Device.Branch != null && c.Device.Branch.Supervision != null
                                      ? c.Device.Branch.Supervision.Name : null,
                })
                .ToListAsync(ct);

            // ---- تبدیل امن رشته به عدد ----
            var units = raw.Select(x =>
            {
                int cur = int.TryParse(x.CurrentCount, out var c) && c >= 0 ? c : 0;
                int cap = int.TryParse(x.TotalCount, out var t) && t >= 0 ? t : 0;

                return new
                {
                    x.Id,
                    x.Name,
                    x.Currency,
                    x.Denomination,
                    x.Type,
                    x.Status,
                    x.DeviceId,
                    x.DeviceIp,
                    x.DeviceName,
                    x.DeviceCode,
                    x.Vendor,
                    x.BranchId,
                    x.BranchName,
                    x.SupervisionId,
                    x.SupervisionName,
                    CurrentCount = cur,
                    TotalCapacity = cap,
                    CurrentAmount = (long)x.Denomination * cur,
                    TotalAmount = (long)x.Denomination * cap,
                    IsLowOrEmpty = x.Status == CashUnitStatus.Empty
                                 || x.Status == CashUnitStatus.Low,
                };
            }).ToList();

            if (filter.OnlyLowOrEmpty)
                units = units.Where(u => u.IsLowOrEmpty).ToList();

            // ---- شاخص‌های کلیدی (قبل از گروه‌بندی) ----
            var result = new CashInventoryResult
            {
                TotalAmount = units.Sum(u => u.CurrentAmount),
                TotalCapacityAmount = units.Sum(u => u.TotalAmount),
                TotalDevices = units.Select(u => u.DeviceId).Distinct().Count(),
                TotalUnits = units.Count,
                EmptyUnits = units.Count(u => u.Status == CashUnitStatus.Empty),
                LowUnits = units.Count(u => u.Status == CashUnitStatus.Low),
            };

            // دستگاه‌هایی که کل موجودی‌شان زیر آستانه است
            if (filter.MinAmountFilter > 0)
            {
                result.DevicesNeedingRefill = units
                    .GroupBy(u => u.DeviceId)
                    .Count(g => g.Sum(u => u.CurrentAmount) < filter.MinAmountFilter);
            }

            // ---- نمودار: توزیع بر اساس ارزش اسکناس ----
            result.ByDenomination = units
                .Where(u => u.Denomination > 0)
                .GroupBy(u => u.Denomination)
                .Select(g => new CashDenominationPoint
                {
                    Denomination = g.Key,
                    UnitCount = g.Count(),
                    NoteCount = g.Sum(u => u.CurrentCount),
                    Amount = g.Sum(u => u.CurrentAmount),
                })
                .OrderByDescending(x => x.Denomination)
                .ToList();

            // ---- گروه‌بندی ----
            result.Rows = filter.GroupBy switch
            {
                CashGroupBy.Device => units
                    .GroupBy(u => new {
                        u.DeviceId,
                        u.DeviceIp,
                        u.DeviceName,
                        u.DeviceCode,
                        u.Vendor,
                        u.BranchName,
                        u.SupervisionName
                    })
                    .Select(g => new CashInventoryRow
                    {
                        DeviceId = g.Key.DeviceId,
                        DeviceIp = g.Key.DeviceIp,
                        DeviceName = g.Key.DeviceName,
                        DeviceCode = g.Key.DeviceCode,
                        Vendor = g.Key.Vendor,
                        BranchName = g.Key.BranchName,
                        SupervisionName = g.Key.SupervisionName,
                        CurrentCount = g.Sum(u => u.CurrentCount),
                        TotalCapacity = g.Sum(u => u.TotalCapacity),
                        CurrentAmount = g.Sum(u => u.CurrentAmount),
                        TotalAmount = g.Sum(u => u.TotalAmount),
                        UnitCount = g.Count(),
                        LowOrEmptyCount = g.Count(u => u.IsLowOrEmpty),
                    })
                    .OrderBy(r => r.CurrentAmount)      // کم‌موجودی‌ها اول
                    .ToList(),

                CashGroupBy.Branch => units
                    .GroupBy(u => new { u.BranchId, u.BranchName, u.SupervisionName })
                    .Select(g => new CashInventoryRow
                    {
                        BranchId = g.Key.BranchId,
                        BranchName = g.Key.BranchName ?? "بدون شعبه",
                        SupervisionName = g.Key.SupervisionName,
                        CurrentCount = g.Sum(u => u.CurrentCount),
                        TotalCapacity = g.Sum(u => u.TotalCapacity),
                        CurrentAmount = g.Sum(u => u.CurrentAmount),
                        TotalAmount = g.Sum(u => u.TotalAmount),
                        UnitCount = g.Count(),
                        LowOrEmptyCount = g.Count(u => u.IsLowOrEmpty),
                    })
                    .OrderByDescending(r => r.CurrentAmount)
                    .ToList(),

                CashGroupBy.Supervision => units
                    .GroupBy(u => new { u.SupervisionId, u.SupervisionName })
                    .Select(g => new CashInventoryRow
                    {
                        SupervisionId = g.Key.SupervisionId,
                        SupervisionName = g.Key.SupervisionName ?? "بدون سرپرستی",
                        CurrentCount = g.Sum(u => u.CurrentCount),
                        TotalCapacity = g.Sum(u => u.TotalCapacity),
                        CurrentAmount = g.Sum(u => u.CurrentAmount),
                        TotalAmount = g.Sum(u => u.TotalAmount),
                        UnitCount = g.Count(),
                        LowOrEmptyCount = g.Count(u => u.IsLowOrEmpty),
                    })
                    .OrderByDescending(r => r.CurrentAmount)
                    .ToList(),

                // تفکیکی — هر کاست یک ردیف
                _ => units
                    .Select(u => new CashInventoryRow
                    {
                        DeviceId = u.DeviceId,
                        DeviceIp = u.DeviceIp,
                        DeviceName = u.DeviceName,
                        DeviceCode = u.DeviceCode,
                        Vendor = u.Vendor,
                        BranchId = u.BranchId,
                        BranchName = u.BranchName,
                        SupervisionId = u.SupervisionId,
                        SupervisionName = u.SupervisionName,
                        UnitName = u.Name,
                        UnitType = u.Type,
                        UnitStatus = u.Status,
                        Currency = u.Currency,
                        Denomination = u.Denomination,
                        CurrentCount = u.CurrentCount,
                        TotalCapacity = u.TotalCapacity,
                        CurrentAmount = u.CurrentAmount,
                        TotalAmount = u.TotalAmount,
                        UnitCount = 1,
                        LowOrEmptyCount = u.IsLowOrEmpty ? 1 : 0,
                    })
                    .OrderBy(r => r.DeviceIp).ThenBy(r => r.UnitName)
                    .ToList(),
            };

            return result;
        }

        // =============================================================
        //  خروجی اکسل
        // =============================================================

        public async Task<byte[]> ExportExcelAsync(
            CashInventoryFilter filter, CancellationToken ct = default)
        {
            var result = await GetInventoryAsync(filter, ct);

            string groupFa = filter.GroupBy switch
            {
                CashGroupBy.Device => "به تفکیک دستگاه",
                CashGroupBy.Branch => "به تفکیک شعبه",
                CashGroupBy.Supervision => "به تفکیک سرپرستی",
                _ => "به تفکیک کاست",
            };

            using var xl = new ReportExcelBuilder("موجودی کاست‌ها");

            xl.AddTitle("گزارش موجودی کاست‌ها", groupFa);

            xl.AddSummary(new[]
            {
                ("مجموع موجودی",  ToMoneyFa(result.TotalAmount)),
                ("ظرفیت کل",      ToMoneyFa(result.TotalCapacityAmount)),
                ("درصد پرشدگی",   result.OverallFillPercent >= 0
                                  ? $"{result.OverallFillPercent}٪" : "—"),
                ("تعداد دستگاه",  result.TotalDevices.ToString("#,##0")),
                ("کاست خالی",     result.EmptyUnits.ToString("#,##0")),
                ("کاست کم",       result.LowUnits.ToString("#,##0")),
            });

            if (filter.GroupBy == CashGroupBy.Cassette)
            {
                xl.AddHeader("ردیف", "کد دستگاه", "آی‌پی", "شعبه", "سرپرستی", "سازنده",
                             "کاست", "نوع", "ارزش اسکناس", "تعداد", "ظرفیت",
                             "درصد پر", "موجودی (ریال)", "وضعیت");

                int i = 1;
                foreach (var r in result.Rows)
                {
                    var tone = r.UnitStatus switch
                    {
                        CashUnitStatus.Empty => RowTone.Error,
                        CashUnitStatus.Jammed => RowTone.Error,
                        CashUnitStatus.Inoperative => RowTone.Error,
                        CashUnitStatus.Low => RowTone.Warning,
                        _ => (RowTone?)null,
                    };

                    xl.AddRow(tone,
                        i++, r.DeviceCode, r.DeviceIp, r.BranchName, r.SupervisionName,
                        r.VendorFa, r.UnitName, r.UnitTypeFa,
                        r.Denomination, r.CurrentCount, r.TotalCapacity,
                        r.HasFillPercent ? $"{r.FillPercent}٪" : "—",
                        r.CurrentAmount, r.UnitStatusFa);
                }
            }
            else
            {
                string firstCol = filter.GroupBy switch
                {
                    CashGroupBy.Device => "دستگاه",
                    CashGroupBy.Branch => "شعبه",
                    _ => "سرپرستی",
                };

                xl.AddHeader("ردیف", firstCol, "شعبه/سرپرستی", "تعداد کاست",
                             "کاست کم/خالی", "تعداد اسکناس", "ظرفیت",
                             "درصد پر", "موجودی (ریال)", "ظرفیت (ریال)");

                int i = 1;
                foreach (var r in result.Rows)
                {
                    var tone = r.LowOrEmptyCount > 0
                        ? (r.LowOrEmptyCount >= r.UnitCount ? RowTone.Error : RowTone.Warning)
                        : (RowTone?)null;

                    string label = filter.GroupBy switch
                    {
                        CashGroupBy.Device => r.DeviceIp ?? "—",
                        CashGroupBy.Branch => r.BranchName ?? "—",
                        _ => r.SupervisionName ?? "—",
                    };

                    string sub = filter.GroupBy switch
                    {
                        CashGroupBy.Device => r.BranchName ?? "—",
                        CashGroupBy.Branch => r.SupervisionName ?? "—",
                        _ => "—",
                    };

                    xl.AddRow(tone,
                        i++, label, sub, r.UnitCount, r.LowOrEmptyCount,
                        r.CurrentCount, r.TotalCapacity,
                        r.HasFillPercent ? $"{r.FillPercent}٪" : "—",
                        r.CurrentAmount, r.TotalAmount);
                }
            }

            xl.Finish();
            return xl.ToBytes();
        }

        // =============================================================
        //  کمکی
        // =============================================================

        private IQueryable<Domain.Entities.Devices.CashUnit> BuildQuery(CashInventoryFilter f)
        {
            var q = _uow.CashUnits.AsNoTracking()
                .Where(c => !c.Deleted && !c.Device.Deleted);

            if (f.BranchId.HasValue)
                q = q.Where(c => c.Device.BranchId == f.BranchId.Value);

            if (f.SupervisionId.HasValue)
                q = q.Where(c => c.Device.Branch != null
                              && c.Device.Branch.SupervisionId == f.SupervisionId.Value);

            if (f.Vendor.HasValue)
                q = q.Where(c => c.Device.Vendor == f.Vendor.Value);

            if (f.UnitType.HasValue)
                q = q.Where(c => c.Type == f.UnitType.Value);

            if (f.UnitStatus.HasValue)
                q = q.Where(c => c.Status == f.UnitStatus.Value);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(c => c.Device.Ip.Contains(s)
                              || (c.Device.Model != null && c.Device.Model.Contains(s))
                              || (c.Name != null && c.Name.Contains(s)));
            }

            return q;
        }

        /// <summary>مبلغ ریالی را به شکل خوانا در می‌آورد</summary>
        public static string ToMoneyFa(long rial)
        {
            if (rial >= 1_000_000_000)
                return $"{rial / 1_000_000_000.0:#,##0.#} میلیارد";

            if (rial >= 1_000_000)
                return $"{rial / 1_000_000.0:#,##0.#} میلیون";

            return rial.ToString("#,##0");
        }
    }
}