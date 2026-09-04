using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    /// <summary>نحوه‌ی گروه‌بندی گزارش موجودی</summary>
    public enum CashGroupBy
    {
        /// <summary>هر کاست یک ردیف</summary>
        Cassette = 0,

        /// <summary>جمع هر دستگاه</summary>
        Device = 1,

        /// <summary>جمع هر شعبه</summary>
        Branch = 2,

        /// <summary>جمع هر سرپرستی</summary>
        Supervision = 3,
    }

    public sealed class CashInventoryFilter : ReportFilterBase
    {
        public CashGroupBy GroupBy { get; set; } = CashGroupBy.Cassette;

        /// <summary>فقط نوع خاصی از کاست</summary>
        public CashUnitType? UnitType { get; set; }

        /// <summary>فقط کاست‌هایی با وضعیت خاص</summary>
        public CashUnitStatus? UnitStatus { get; set; }

        /// <summary>
        /// فقط دستگاه‌هایی که موجودی‌شان کمتر از این مبلغ است (ریال).
        /// صفر یعنی بدون فیلتر.
        /// </summary>
        public long MinAmountFilter { get; set; } = 0;

        /// <summary>فقط کاست‌های خالی یا کم</summary>
        public bool OnlyLowOrEmpty { get; set; }
    }

    /// <summary>یک ردیف گزارش — بسته به GroupBy معنای متفاوتی دارد</summary>
    public sealed class CashInventoryRow
    {
        // ---- شناسه ----
        public Guid? DeviceId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? SupervisionId { get; set; }

        // ---- نمایش ----
        public string? DeviceIp { get; set; }
        public string? DeviceName { get; set; }
        public int? DeviceCode { get; set; }
        public string? BranchName { get; set; }
        public string? SupervisionName { get; set; }
        public DeviceVendor Vendor { get; set; }

        // ---- کاست (فقط در حالت تفکیکی) ----
        public string? UnitName { get; set; }
        public CashUnitType UnitType { get; set; }
        public CashUnitStatus UnitStatus { get; set; }
        public string? Currency { get; set; }
        public int Denomination { get; set; }

        // ---- اعداد ----
        public int CurrentCount { get; set; }
        public int TotalCapacity { get; set; }
        public long CurrentAmount { get; set; }
        public long TotalAmount { get; set; }

        /// <summary>تعداد کاست‌های تجمیع‌شده در این ردیف</summary>
        public int UnitCount { get; set; }

        /// <summary>تعداد کاست‌های خالی یا کم</summary>
        public int LowOrEmptyCount { get; set; }

        // ---- محاسباتی ----
        public int FillPercent
        {
            get
            {
                if (TotalCapacity <= 0) return -1;
                if (CurrentCount <= 0) return 0;
                return (int)Math.Clamp(CurrentCount * 100.0 / TotalCapacity, 0, 100);
            }
        }

        public bool HasFillPercent => TotalCapacity > 0;

        public string UnitTypeFa => UnitType switch
        {
            CashUnitType.Bill => "پرداختی",
            CashUnitType.Reject => "ریجکت",
            CashUnitType.Recycle => "بازیافتی",
            CashUnitType.Deposited => "واریزی",
            CashUnitType.Retract => "بازگردانی",
            _ => "—",
        };

        public string UnitStatusFa => UnitStatus switch
        {
            CashUnitStatus.Ok => "سالم",
            CashUnitStatus.Low => "کم",
            CashUnitStatus.Empty => "خالی",
            CashUnitStatus.Full => "پر",
            CashUnitStatus.Jammed => "گیر کرده",
            CashUnitStatus.Inoperative => "خارج از کار",
            CashUnitStatus.Missing => "موجود نیست",
            _ => "نامشخص",
        };

        public string VendorFa => Vendor switch
        {
            DeviceVendor.Hyosung => "هیوسانگ",
            DeviceVendor.GRG => "GRG",
            DeviceVendor.Other => "سایر",
            _ => "—",
        };
    }

    public sealed class CashInventoryResult
    {
        public List<CashInventoryRow> Rows { get; set; } = new();

        // ---- شاخص‌های کلیدی ----
        public long TotalAmount { get; set; }
        public long TotalCapacityAmount { get; set; }
        public int TotalDevices { get; set; }
        public int TotalUnits { get; set; }
        public int EmptyUnits { get; set; }
        public int LowUnits { get; set; }

        /// <summary>دستگاه‌هایی که کل موجودی‌شان زیر آستانه است</summary>
        public int DevicesNeedingRefill { get; set; }

        /// <summary>درصد پرشدگی کل ناوگان</summary>
        public int OverallFillPercent =>
            TotalCapacityAmount <= 0 ? -1
            : (int)Math.Clamp(TotalAmount * 100.0 / TotalCapacityAmount, 0, 100);

        /// <summary>برای نمودار — توزیع مبلغ به تفکیک ارزش اسکناس</summary>
        public List<CashDenominationPoint> ByDenomination { get; set; } = new();
    }

    public sealed class CashDenominationPoint
    {
        public int Denomination { get; set; }
        public int UnitCount { get; set; }
        public int NoteCount { get; set; }
        public long Amount { get; set; }
    }
}