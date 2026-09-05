using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    /// <summary>فیلتر گزارش آماده‌به‌کاری</summary>
    public sealed class DeviceAvailabilityFilter : ReportFilterBase
    {
        /// <summary>محدود کردن به دستگاه‌هایی که وضعیت خاصی داشته‌اند</summary>
        public AvailabilityState? State { get; set; }

        /// <summary>
        /// حداقل مدت یک بازه‌ی خارج از سرویس برای شمرده شدن در «تعداد
        /// دفعات خروج».
        ///
        /// همه‌ی بازه‌ها ثبت می‌شوند؛ این فقط روی نمایش اثر دارد — همان
        /// اصلی که در گزارش خرابی قطعات هم رعایت شد. اگر بانک بعداً آستانه
        /// را عوض کند، داده‌ی گذشته دست‌نخورده باقی می‌ماند.
        ///
        /// روی **مدت** اثری ندارد؛ فقط روی **شمارش** دفعات. یک قطعی
        /// دودقیقه‌ای در مدت کل حساب می‌شود ولی به‌عنوان «یک بار خروج از
        /// سرویس» شمرده نمی‌شود.
        /// </summary>
        public int MinOutageMinutes { get; set; } = 5;

        /// <summary>
        /// آیا مدت قطعی سرور از مخرج کسر شود؟
        ///
        /// پیش‌فرض true. اگر false شود، بازه‌هایی که سرور خاموش بوده به
        /// آخرین وضعیت شناخته‌شده‌ی هر دستگاه نسبت داده می‌شوند — که برای
        /// حسابرسی «چه چیزی واقعاً ثبت شد» ممکن است لازم باشد.
        /// </summary>
        public bool ExcludeServerOutages { get; set; } = true;

        /// <summary>فقط دستگاه‌هایی که در حال حاضر آماده‌به‌کار نیستند</summary>
        public bool OnlyProblematic { get; set; }
    }

    /// <summary>یک ردیف — به‌ازای هر دستگاه</summary>
    public sealed class DeviceAvailabilityRow
    {
        public Guid DeviceId { get; set; }
        public string DeviceIp { get; set; } = "";
        public string? DeviceName { get; set; }
        public int? DeviceCode { get; set; }
        public string? BranchName { get; set; }
        public string? SupervisionName { get; set; }
        public DeviceVendor Vendor { get; set; }

        // ---- مدت هر وضعیت به ثانیه ----
        public long AvailableSeconds { get; set; }
        public long OutOfServiceSeconds { get; set; }
        public long ErrorSeconds { get; set; }
        public long DisconnectedSeconds { get; set; }
        public long UnknownSeconds { get; set; }

        /// <summary>
        /// مجموع مدتی که واقعاً رصد شده — مخرج کسر.
        ///
        /// عمداً «طول بازه‌ی درخواستی» نیست. دستگاهی که وسط ماه نصب شده
        /// نباید به‌خاطر روزهای قبل از نصبش صفر درصد بگیرد.
        /// </summary>
        public long ObservedSeconds { get; set; }

        public int OutageCount { get; set; }
        public int LongestOutageSeconds { get; set; }
        public DateTime? LastOutageAt { get; set; }

        public AvailabilityState CurrentState { get; set; }
        public DateTime? CurrentStateSince { get; set; }

        /// <summary>اگر false باشد، در این بازه هیچ داده‌ای از دستگاه نداریم</summary>
        public bool HasData { get; set; }

        /// <summary>
        /// درصد آماده‌به‌کاری.
        ///
        /// Unknown از مخرج حذف می‌شود — «نمی‌دانیم» نه به نفع دستگاه است
        /// نه به ضررش. اگر در مخرج بماند، دستگاهی که مدتی گزارش نداده
        /// جریمه می‌شود بابت چیزی که شاید اصلاً اتفاق نیفتاده.
        /// </summary>
        public double AvailabilityPercent
        {
            get
            {
                long denom = ObservedSeconds - UnknownSeconds;
                if (denom <= 0) return 0;
                return Math.Round(AvailableSeconds * 100.0 / denom, 2);
            }
        }

        /// <summary>سهم هر دلیل از بی‌کاری — برای اینکه معلوم شود تقصیر کیست</summary>
        public long TotalDownSeconds =>
            OutOfServiceSeconds + ErrorSeconds + DisconnectedSeconds;

        public string VendorFa => Vendor switch
        {
            DeviceVendor.Hyosung => "هیوسانگ",
            DeviceVendor.GRG => "GRG",
            DeviceVendor.Other => "سایر",
            _ => "—",
        };
    }

    /// <summary>یک بازه‌ی منفرد — نمای تفصیلی یک دستگاه</summary>
    public sealed class DeviceStateDetailRow
    {
        public Guid Id { get; set; }
        public AvailabilityState State { get; set; }
        public string StateFa { get; set; } = "";
        public DeviceMode StartMode { get; set; }
        public DeviceMode CurrentMode { get; set; }
        public string ModeFa { get; set; } = "";

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public int TransitionCount { get; set; }

        public bool IsOpen => EndedAt is null;
    }

    /// <summary>خروجی کامل گزارش</summary>
    public sealed class DeviceAvailabilityResult
    {
        public List<DeviceAvailabilityRow> Rows { get; set; } = new();

        /// <summary>درصد آماده‌به‌کاری کل ناوگان — وزنی بر اساس مدت، نه میانگین ساده</summary>
        public double FleetAvailabilityPercent { get; set; }

        public int DeviceCount { get; set; }
        public int DevicesWithoutData { get; set; }
        public int CurrentlyDown { get; set; }
        public int TotalOutages { get; set; }

        public long TotalAvailableSeconds { get; set; }
        public long TotalOutOfServiceSeconds { get; set; }
        public long TotalErrorSeconds { get; set; }
        public long TotalDisconnectedSeconds { get; set; }

        /// <summary>مدت قطعی سرور که از محاسبه کنار گذاشته شد</summary>
        public long ExcludedServerOutageSeconds { get; set; }

        /// <summary>برای نمودار — سهم هر دلیل از بی‌کاری</summary>
        public List<AvailabilityChartPoint> ByReason { get; set; } = new();
    }

    public sealed class AvailabilityChartPoint
    {
        public string LabelFa { get; set; } = "";
        public long Seconds { get; set; }
    }
}