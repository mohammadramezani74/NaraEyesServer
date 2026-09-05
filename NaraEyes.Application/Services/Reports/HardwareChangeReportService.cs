using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Common.Excel;
using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Services.Reports
{
    public interface IHardwareChangeReportService
    {
        Task<HardwareChangeResult> GetAsync(
            HardwareChangeFilter filter, CancellationToken ct = default);

        Task<byte[]> ExportExcelAsync(
            HardwareChangeFilter filter, CancellationToken ct = default);
        Task<byte[]> ExportFleetExcelAsync(
    HardwareChangeFilter filter,
    bool onlyOutliers,
    string? search,
    CancellationToken ct = default);
    }

    public class HardwareChangeReportService : IHardwareChangeReportService
    {
        private readonly IApplicationUnitOfWork _uow;

        public HardwareChangeReportService(IApplicationUnitOfWork uow) => _uow = uow;

        // =============================================================
        //  گزارش
        // =============================================================

        public async Task<HardwareChangeResult> GetAsync(
            HardwareChangeFilter filter, CancellationToken ct = default)
        {
            var from = filter.FromDate;
            var to = filter.ToDate;

            var q = _uow.DeviceHardwareChanges.AsNoTracking()
                .Where(x => x.DetectedAt >= from && x.DetectedAt <= to);

            if (filter.Component.HasValue)
                q = q.Where(x => x.Component == filter.Component.Value);

            if (filter.Kind.HasValue)
                q = q.Where(x => x.Kind == filter.Kind.Value);

            if (filter.OnlyDowngrades)
                q = q.Where(x => x.Kind == HardwareChangeKind.Downgrade);

            if (filter.BranchId.HasValue)
                q = q.Where(x => x.Device.BranchId == filter.BranchId.Value);

            if (filter.SupervisionId.HasValue)
                q = q.Where(x => x.Device.Branch != null
                              && x.Device.Branch.SupervisionId == filter.SupervisionId.Value);

            if (filter.Vendor.HasValue)
                q = q.Where(x => x.Device.Vendor == filter.Vendor.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                q = q.Where(x => x.Device.Ip.Contains(s)
                              || x.Description.Contains(s)
                              || (x.Device.Model != null && x.Device.Model.Contains(s))
                              || (x.Device.Branch != null && x.Device.Branch.Name.Contains(s)));
            }

            var rows = await q
                .OrderByDescending(x => x.DetectedAt)
                .Select(x => new HardwareChangeRow
                {
                    Id = x.Id,
                    DeviceId = x.DeviceId,
                    DeviceIp = x.Device.Ip,
                    DeviceCode = x.Device.Code,
                    BranchName = x.Device.Branch != null ? x.Device.Branch.Name : null,
                    SupervisionName = x.Device.Branch != null && x.Device.Branch.Supervision != null
                        ? x.Device.Branch.Supervision.Name : null,
                    Vendor = x.Device.Vendor,
                    Component = x.Component,
                    Kind = x.Kind,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    Description = x.Description,
                    DetectedAt = x.DetectedAt,
                })
                .ToListAsync(ct);

            var byComponent = rows
                .GroupBy(r => r.Component)
                .Select(g => new HardwareComponentCount
                {
                    Component = g.Key,
                    LabelFa = g.First().ComponentFa,
                    Total = g.Count(),
                    Downgrades = g.Count(x => x.Kind == HardwareChangeKind.Downgrade),
                })
                .OrderByDescending(x => x.Downgrades)
                .ThenByDescending(x => x.Total)
                .ToList();

            return new HardwareChangeResult
            {
                Rows = rows,
                TotalChanges = rows.Count,
                Downgrades = rows.Count(r => r.Kind == HardwareChangeKind.Downgrade),
                Replacements = rows.Count(r => r.Kind == HardwareChangeKind.Replaced),
                Upgrades = rows.Count(r => r.Kind == HardwareChangeKind.Upgrade),
                AffectedDevices = rows.Select(r => r.DeviceId).Distinct().Count(),
                ByComponent = byComponent,
                FleetProfiles = await GetFleetProfilesAsync(filter, ct),
                DevicesWithoutProfile = await CountDevicesWithoutProfileAsync(filter, ct),
            };
        }

        // =============================================================
        //  پروفایل فعلی ناوگان
        // =============================================================

        /// <summary>
        /// ترکیب‌های مشخصات موجود در ناوگان، مرتب بر اساس تعداد.
        ///
        /// چرا لازم است: مبنای مقایسه از **اولین اجرای ایجنت** گرفته
        /// می‌شود. اگر کارشناسی پیش از نصب سامانه رم دستگاهی را از ۴ به
        /// ۲ گیگ تنزل داده باشد، ما آن ۲ گیگ را «وضعیت عادی» ثبت می‌کنیم
        /// و هیچ‌وقت آلارمی نمی‌دهد — یعنی دقیقاً همان تعویض‌هایی که باعث
        /// شد بانک این درخواست را بدهد.
        ///
        /// تنها راه پیدا کردنشان مقایسه‌ی دستگاه‌ها با یکدیگر است. ناوگان
        /// یکدست است (SA93 / i5-3570 / 4GB / 1TB)، پس ترکیبی که فقط چند
        /// دستگاه دارد در کنار ترکیبی با ۲۹۰ دستگاه، مشکوک است.
        ///
        /// ⚠️ این یک **قرینه** است، نه اثبات — ممکن است چند دستگاه از اول
        /// متفاوت خریداری شده باشند.
        ///
        /// این بخش عمداً به بازه‌ی تاریخ محدود نیست و همیشه وضعیت امروز
        /// را نشان می‌دهد، چون هدفش چیزی است که هیچ ردی در تاریخچه ندارد.
        /// </summary>
        private async Task<List<FleetProfileGroup>> GetFleetProfilesAsync(
            HardwareChangeFilter filter, CancellationToken ct)
        {
            var q = _uow.DeviceHardwareProfiles.AsNoTracking().AsQueryable();

            if (filter.BranchId.HasValue)
                q = q.Where(x => x.Device.BranchId == filter.BranchId.Value);

            if (filter.SupervisionId.HasValue)
                q = q.Where(x => x.Device.Branch != null
                              && x.Device.Branch.SupervisionId == filter.SupervisionId.Value);

            if (filter.Vendor.HasValue)
                q = q.Where(x => x.Device.Vendor == filter.Vendor.Value);

            var raw = await q
                .Select(x => new
                {
                    x.RamTotalMb,
                    x.CpuName,
                    x.CpuCores,
                    x.DiskSizeBytes,
                    x.BoardProduct,
                    Ip = x.Device.Ip,
                    Code = x.Device.Code,
                    Branch = x.Device.Branch != null ? x.Device.Branch.Name : null,
                })
                .ToListAsync(ct);

            // گروه‌بندی در حافظه انجام می‌شود چون تعداد دستگاه‌ها ۳۰۰ است
            // و گروه‌بندی روی پنج ستون در SQL خوانایی را بدون سود ملموسی
            // از بین می‌برد.
            var groups = raw
                .GroupBy(x => new
                {
                    x.RamTotalMb,
                    x.CpuName,
                    x.CpuCores,

                    // گرد کردن به گیگابایت. ظرفیت گزارش‌شده‌ی دیسک بسته به
                    // درایور چند مگابایت جابه‌جا می‌شود و بدون گرد کردن،
                    // دستگاه‌های یکسان در گروه‌های جدا می‌افتند و کل این
                    // جدول بی‌فایده می‌شود.
                    DiskGb = x.DiskSizeBytes / 1_000_000_000,

                    x.BoardProduct,
                })
                .Select(g => new FleetProfileGroup
                {
                    RamTotalMb = g.Key.RamTotalMb,
                    CpuName = g.Key.CpuName,
                    CpuCores = g.Key.CpuCores,
                    DiskSizeBytes = g.Key.DiskGb * 1_000_000_000,
                    BoardProduct = g.Key.BoardProduct,
                    DeviceCount = g.Count(),

                    // همیشه پر می‌شود، نه فقط برای گروه‌های کوچک. کاربر
                    // باید از هر گروه بتواند به دستگاه‌هایش برسد.
                    Devices = g.Select(x => new FleetProfileDevice
                    {
                        Ip = x.Ip,
                        Code = x.Code,
                        BranchName = x.Branch,
                    })
                    .OrderBy(x => x.Ip)
                    .ToList(),
                })
                .OrderByDescending(x => x.DeviceCount)
                .ToList();

            if (groups.Count > 0) groups[0].IsMajority = true;

            return groups;
        }

        private async Task<int> CountDevicesWithoutProfileAsync(
            HardwareChangeFilter filter, CancellationToken ct)
        {
            var withProfile = _uow.DeviceHardwareProfiles.AsNoTracking()
                .Select(x => x.DeviceId);

            var q = _uow.Devices.AsNoTracking()
                .Where(d => !withProfile.Contains(d.Id));

            if (filter.BranchId.HasValue)
                q = q.Where(d => d.BranchId == filter.BranchId.Value);

            if (filter.SupervisionId.HasValue)
                q = q.Where(d => d.Branch != null
                              && d.Branch.SupervisionId == filter.SupervisionId.Value);

            if (filter.Vendor.HasValue)
                q = q.Where(d => d.Vendor == filter.Vendor.Value);

            return await q.CountAsync(ct);
        }

        // =============================================================
        //  اکسل
        // =============================================================

        public async Task<byte[]> ExportExcelAsync(
            HardwareChangeFilter filter, CancellationToken ct = default)
        {
            var result = await GetAsync(filter, ct);

            using var xl = new ReportExcelBuilder("تغییرات سخت‌افزاری");

            xl.AddTitle(
                "گزارش تغییرات سخت‌افزاری",
                $"از {ReportExcelBuilder.ToJalali(filter.FromDate)} " +
                $"تا {ReportExcelBuilder.ToJalali(filter.ToDate)}");

            xl.AddSummary(new[]
            {
                ("کل تغییرات",       result.TotalChanges.ToString("#,##0")),
                ("تنزل",             result.Downgrades.ToString("#,##0")),
                ("تعویض هم‌سطح",     result.Replacements.ToString("#,##0")),
                ("ارتقا",            result.Upgrades.ToString("#,##0")),
                ("دستگاه‌های متأثر", result.AffectedDevices.ToString("#,##0")),
                ("بدون پروفایل",     result.DevicesWithoutProfile.ToString("#,##0")),
            });

            // ---------- جدول ۱: تغییرات ثبت‌شده ----------
            xl.AddHeader(
                "ردیف", "تاریخ", "کد دستگاه", "آی‌پی", "شعبه", "سرپرستی", "سازنده",
                "قطعه", "نوع تغییر", "مقدار قبلی", "مقدار جدید", "شرح");

            int i = 1;
            foreach (var r in result.Rows)
            {
                RowTone? tone = null;
                if (r.Kind == HardwareChangeKind.Downgrade) tone = RowTone.Error;
                else if (r.Kind == HardwareChangeKind.Replaced) tone = RowTone.Warning;

                xl.AddRow(tone,
                    i++,
                    ReportExcelBuilder.ToJalali(r.DetectedAt, withTime: true),
                    r.DeviceCode,
                    r.DeviceIp,
                    r.BranchName,
                    r.SupervisionName,
                    r.VendorFa,
                    r.ComponentFa,
                    r.KindFa,
                    r.OldValue,
                    r.NewValue,
                    r.Description);
            }

            // ---------- جدول ۲ و ۳: پروفایل ناوگان ----------
            //
            // در همان شیت و بعد از یک فاصله، چون کاربر بانک معمولاً کل
            // فایل را چاپ می‌کند و شیت دوم را نمی‌بیند.
          

            xl.Finish();
            return xl.ToBytes();
        }

        /// <summary>
        /// خروجی اکسل تب «پروفایل فعلی ناوگان».
        ///
        /// فیلترهای این تب سمت کلاینت اعمال می‌شوند، پس باید صریح پاس
        /// داده شوند — وگرنه کاربر «فقط متفاوت‌ها» را می‌بیند ولی فایلی
        /// با کل ناوگان می‌گیرد و متوجه هم نمی‌شود.
        /// </summary>
        public async Task<byte[]> ExportFleetExcelAsync(
            HardwareChangeFilter filter,
            bool onlyOutliers,
            string? search,
            CancellationToken ct = default)
        {
            var groups = await GetFleetProfilesAsync(filter, ct);

            using var xl = new ReportExcelBuilder("پروفایل ناوگان");

            xl.AddTitle(
                "پروفایل فعلی سخت‌افزار ناوگان",
                "دستگاهی که مشخصاتش با اکثریت فرق دارد، احتمالاً پیش از نصب سامانه دستکاری شده — " +
                "این یک قرینه است، نه اثبات");

            int totalDevices = groups.Sum(g => g.DeviceCount);
            int outliers = groups.Where(g => !g.IsMajority).Sum(g => g.DeviceCount);

            xl.AddSummary(new[]
            {
                ("تعداد دستگاه",       totalDevices.ToString("#,##0")),
                ("تعداد ترکیب",        groups.Count.ToString("#,##0")),
                ("متفاوت با اکثریت",   outliers.ToString("#,##0")),
                ("فیلتر اعمال‌شده",    onlyOutliers ? "فقط متفاوت‌ها" : "همه"),
            });

            //xl.AddTitle("دستگاه‌ها به تفکیک پروفایل");

            xl.AddHeader(
                "ردیف", "کد دستگاه", "آی‌پی", "شعبه",
                "حافظه", "پردازنده", "هسته", "هارد", "مادربرد", "وضعیت");

            var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            int k = 1;
            foreach (var g in groups.OrderBy(x => x.IsMajority ? 1 : 0))
            {
                if (onlyOutliers && g.IsMajority) continue;

                foreach (var d in g.Devices)
                {
                    if (s is not null && !MatchesSearch(d, g, s)) continue;

                    xl.AddRow(g.IsMajority ? null : RowTone.Warning,
                        k++,
                        d.Code?.ToString(),
                        d.Ip,
                        d.BranchName,
                        g.RamFa,
                        g.CpuName,
                        g.CpuCores,
                        g.DiskFa,
                        g.BoardProduct,
                        g.IsMajority ? "اکثریت" : "متفاوت با اکثریت");
                }
            }

            xl.Finish();
            return xl.ToBytes();
        }

        private static bool MatchesSearch(
            FleetProfileDevice d, FleetProfileGroup g, string s)
        {
            if (d.Ip.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;
            if (d.Code is not null && d.Code.ToString()!.Contains(s)) return true;
            if (d.BranchName is not null
                && d.BranchName.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;
            if (g.CpuName is not null
                && g.CpuName.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;
            if (g.BoardProduct is not null
                && g.BoardProduct.Contains(s, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}