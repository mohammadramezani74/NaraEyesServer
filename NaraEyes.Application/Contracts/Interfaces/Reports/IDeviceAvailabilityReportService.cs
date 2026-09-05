using NaraEyes.Application.Contracts.Models.Reports;

namespace NaraEyes.Application.Contracts.Interfaces.Reports
{
    public interface IDeviceAvailabilityReportService
    {
        Task<DeviceAvailabilityResult> GetSummaryAsync(
            DeviceAvailabilityFilter filter, CancellationToken ct = default);

        Task<List<DeviceStateDetailRow>> GetDetailsAsync(
            Guid deviceId, DeviceAvailabilityFilter filter, CancellationToken ct = default);

        Task<byte[]> ExportExcelAsync(
            DeviceAvailabilityFilter filter, CancellationToken ct = default);
    }
}