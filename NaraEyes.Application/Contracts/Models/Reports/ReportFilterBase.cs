using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    /// <summary>
    /// فیلترهای مشترک همه‌ی گزارش‌ها.
    /// هر گزارش می‌تواند از این ارث ببرد و فیلترهای اختصاصی خودش را اضافه کند.
    ///
    /// عمداً از DateRange مادبلیزر استفاده نشده تا لایه‌ی Application
    /// به کتابخانه‌ی رابط کاربری وابسته نشود.
    /// </summary>
    public class ReportFilterBase
    {
        /// <summary>ابتدای بازه — پیش‌فرض یک ماه قبل</summary>
        public DateTime? From { get; set; } = DateTime.Now.Date.AddMonths(-1);

        /// <summary>انتهای بازه — پیش‌فرض امروز</summary>
        public DateTime? To { get; set; } = DateTime.Now.Date;

        public Guid? SupervisionId { get; set; }
        public Guid? BranchId { get; set; }
        public DeviceVendor? Vendor { get; set; }

        /// <summary>جستجوی آزاد — آی‌پی، نام یا سریال دستگاه</summary>
        public string? Search { get; set; }

        // ---- صفحه‌بندی ----
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        // ---- مرتب‌سازی ----
        public string? SortLabel { get; set; }
        public bool SortDescending { get; set; }

        /// <summary>ابتدای بازه، آماده برای استفاده در کوئری</summary>
        public DateTime FromDate =>
            (From ?? DateTime.Now.Date.AddMonths(-1)).Date;

        /// <summary>انتهای بازه — تا آخرین لحظه‌ی همان روز، نه ساعت ۰۰:۰۰</summary>
        public DateTime ToDate =>
            (To ?? DateTime.Now.Date).Date.AddDays(1).AddTicks(-1);
    }
}