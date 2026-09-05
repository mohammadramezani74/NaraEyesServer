using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    public sealed class HardwareChangeFilter : ReportFilterBase
    {
        public HardwareComponent? Component { get; set; }
        public HardwareChangeKind? Kind { get; set; }

        /// <summary>
        /// فقط تنزل‌ها — همان چیزی که بانک شکایت داشت.
        /// پیش‌فرض false تا کاربر تصویر کامل را ببیند، ولی معمولاً
        /// اولین کاری که می‌کند روشن کردن همین است.
        /// </summary>
        public bool OnlyDowngrades { get; set; }
    }

    public sealed class HardwareChangeRow
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceIp { get; set; } = "";
        public int? DeviceCode { get; set; }
        public string? BranchName { get; set; }
        public string? SupervisionName { get; set; }
        public DeviceVendor Vendor { get; set; }

        public HardwareComponent Component { get; set; }
        public HardwareChangeKind Kind { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string Description { get; set; } = "";

        public DateTime DetectedAt { get; set; }

        public string ComponentFa => Component switch
        {
            HardwareComponent.Ram => "حافظه",
            HardwareComponent.Cpu => "پردازنده",
            HardwareComponent.Disk => "هارد دیسک",
            HardwareComponent.Motherboard => "مادربرد",
            _ => "—",
        };

        public string KindFa => Kind switch
        {
            HardwareChangeKind.Downgrade => "تنزل",
            HardwareChangeKind.Replaced => "تعویض",
            HardwareChangeKind.Upgrade => "ارتقا",
            _ => "—",
        };

        public string VendorFa => Vendor switch
        {
            DeviceVendor.Hyosung => "هیوسانگ",
            DeviceVendor.GRG => "GRG",
            DeviceVendor.Other => "سایر",
            _ => "—",
        };
    }

    /// <summary>
    /// یک ترکیب مشخصات و تعداد دستگاه‌هایی که آن را دارند.
    ///
    /// چرا لازم است: مبنا از اولین اجرای ایجنت گرفته می‌شود، پس اگر
    /// کارشناسی **قبل از نصب سامانه** قطعه‌ای را تنزل داده باشد، آن را
    /// «وضعیت عادی» ثبت کرده‌ایم و هیچ آلارمی نمی‌دهد.
    ///
    /// تنها راه پیدا کردنش این است: چون ناوگان یکدست است (SA93 / i5-3570
    /// / 4GB / 1TB)، دستگاهی که ۲ گیگ رم دارد در حالی که ۲۹۰ تای دیگر
    /// ۴ گیگ دارند تقریباً قطعاً قبلاً دستکاری شده.
    /// </summary>
    public sealed class FleetProfileGroup
    {
        public int RamTotalMb { get; set; }
        public string? CpuName { get; set; }
        public int CpuCores { get; set; }
        public long DiskSizeBytes { get; set; }
        public string? BoardProduct { get; set; }

        public int DeviceCount { get; set; }

        /// <summary>دستگاه‌های این گروه — همیشه پر می‌شود</summary>
        public List<FleetProfileDevice> Devices { get; set; } = new();

        /// <summary>پرتکرارترین ترکیب ناوگان؟</summary>
        public bool IsMajority { get; set; }

        public string RamFa => (RamTotalMb / 1024.0).ToString("0.##") + " گیگابایت";
        public string DiskFa => (DiskSizeBytes / 1000.0 / 1000.0 / 1000.0).ToString("0") + " گیگابایت";
    }
    public sealed class FleetProfileDevice
    {
        public string Ip { get; set; } = "";
        public int? Code { get; set; }
        public string? BranchName { get; set; }
    }
    public sealed class HardwareChangeResult
    {
        public List<HardwareChangeRow> Rows { get; set; } = new();

        public int TotalChanges { get; set; }
        public int Downgrades { get; set; }
        public int Replacements { get; set; }
        public int Upgrades { get; set; }
        public int AffectedDevices { get; set; }

        /// <summary>تعداد تغییر به تفکیک قطعه — برای نمودار</summary>
        public List<HardwareComponentCount> ByComponent { get; set; } = new();

        public List<FleetProfileGroup> FleetProfiles { get; set; } = new();

        /// <summary>دستگاه‌هایی که هنوز پروفایلی از آن‌ها نداریم</summary>
        public int DevicesWithoutProfile { get; set; }
    }

    public sealed class HardwareComponentCount
    {
        public HardwareComponent Component { get; set; }
        public string LabelFa { get; set; } = "";
        public int Downgrades { get; set; }
        public int Total { get; set; }
    }
}