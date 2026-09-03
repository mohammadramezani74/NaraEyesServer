using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    /// <summary>فیلتر گزارش خرابی قطعات</summary>
    public sealed class ModuleFaultFilter : ReportFilterBase
    {
        /// <summary>محدود کردن به یک ماژول خاص</summary>
        public DeviceModuleType? Module { get; set; }

        /// <summary>
        /// حداقل مدت خرابی برای نمایش در گزارش.
        ///
        /// همه‌ی خرابی‌ها در دیتابیس ثبت می‌شوند؛ این فیلتر فقط روی نمایش
        /// اثر دارد. بنابراین اگر بعداً آستانه عوض شود، داده‌ی گذشته از
        /// دست نمی‌رود.
        /// </summary>
        public int MinDurationMinutes { get; set; } = 30;

        /// <summary>فقط خرابی‌هایی که هنوز برطرف نشده‌اند</summary>
        public bool OnlyOpen { get; set; }
    }

    /// <summary>یک ردیف خلاصه — به‌ازای هر دستگاه × ماژول</summary>
    public sealed class ModuleFaultSummaryRow
    {
        public Guid DeviceId { get; set; }
        public string DeviceIp { get; set; } = "";
        public string? DeviceName { get; set; }
        public int? DeviceCode { get; set; }
        public string? BranchName { get; set; }
        public string? SupervisionName { get; set; }
        public DeviceVendor Vendor { get; set; }

        public DeviceModuleType Module { get; set; }
        public string ModuleFa { get; set; } = "";

        public int FaultCount { get; set; }
        public long TotalDownSeconds { get; set; }
        public int MaxDownSeconds { get; set; }
        public DateTime? LastFaultAt { get; set; }
        public bool HasOpenFault { get; set; }

        public double AvgDownMinutes =>
            FaultCount == 0 ? 0 : Math.Round(TotalDownSeconds / 60.0 / FaultCount, 1);

        public string VendorFa => Vendor switch
        {
            DeviceVendor.Hyosung => "هیوسانگ",
            DeviceVendor.GRG => "GRG",
            DeviceVendor.Other => "سایر",
            _ => "—",
        };
    }

    /// <summary>یک خرابی منفرد — برای نمای تفصیلی</summary>
    public sealed class ModuleFaultDetailRow
    {
        public Guid Id { get; set; }
        public string DeviceIp { get; set; } = "";
        public string? BranchName { get; set; }
        public DeviceModuleType Module { get; set; }
        public string ModuleFa { get; set; } = "";

        public HealthStatus StartStatus { get; set; }
        public HealthStatus CurrentStatus { get; set; }
        public string StatusFa { get; set; } = "";
        public string? Detail { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int DurationSeconds { get; set; }
        public int TransitionCount { get; set; }

        public bool IsOpen => ResolvedAt is null;
    }

    /// <summary>خروجی کامل گزارش — خلاصه + شاخص‌های کلیدی</summary>
    public sealed class ModuleFaultReportResult
    {
        public List<ModuleFaultSummaryRow> Rows { get; set; } = new();

        public int TotalFaults { get; set; }
        public long TotalDownSeconds { get; set; }
        public int OpenFaults { get; set; }
        public int AffectedDevices { get; set; }

        /// <summary>ماژولی که بیشترین تعداد خرابی را داشته</summary>
        public string? WorstModuleFa { get; set; }

        /// <summary>برای نمودار — تعداد خرابی به تفکیک ماژول</summary>
        public List<ModuleFaultChartPoint> ByModule { get; set; } = new();
    }

    public sealed class ModuleFaultChartPoint
    {
        public string ModuleFa { get; set; } = "";
        public int Count { get; set; }
        public long DownSeconds { get; set; }
    }
}