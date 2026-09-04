using NaraEyes.Application.Contracts.Models.Reports;

namespace NaraEyes.Application.Contracts.Interfaces.Reports
{
    public interface ICashInventoryReportService
    {
        /// <summary>
        /// موجودی کاست‌ها با گروه‌بندی دلخواه:
        /// تفکیکی (هر کاست)، یا تجمیعی بر اساس دستگاه / شعبه / سرپرستی.
        /// </summary>
        Task<CashInventoryResult> GetInventoryAsync(
            CashInventoryFilter filter, CancellationToken ct = default);

        /// <summary>خروجی اکسل با قالب‌بندی</summary>
        Task<byte[]> ExportExcelAsync(
            CashInventoryFilter filter, CancellationToken ct = default);
    }
}