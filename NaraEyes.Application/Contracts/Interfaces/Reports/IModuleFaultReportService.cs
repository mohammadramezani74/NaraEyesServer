using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Interfaces.Reports
{
    public interface IModuleFaultReportService
    {
        /// <summary>خلاصه‌ی خرابی‌ها — یک ردیف به‌ازای هر دستگاه × ماژول</summary>
        Task<ModuleFaultReportResult> GetSummaryAsync(
            ModuleFaultFilter filter, CancellationToken ct = default);

        /// <summary>جزئیات تک‌تک خرابی‌های یک دستگاه × ماژول</summary>
        Task<List<ModuleFaultDetailRow>> GetDetailsAsync(
            Guid deviceId, DeviceModuleType module,
            ModuleFaultFilter filter, CancellationToken ct = default);

        /// <summary>خروجی اکسل با قالب‌بندی</summary>
        Task<byte[]> ExportExcelAsync(
            ModuleFaultFilter filter, CancellationToken ct = default);
    }
}