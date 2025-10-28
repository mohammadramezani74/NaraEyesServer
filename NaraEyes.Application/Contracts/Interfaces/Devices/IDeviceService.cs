using Microsoft.AspNetCore.Components.Forms;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Devices
{
    public interface IDeviceService
    {
        /// <summary>ثبت یک ایجنت جدید</summary>
        Task<Guid> RegisterAsync(RegisterDeviceCommand context, CancellationToken ct);

        Task<Guid> ReRegisterAsync(string ip, string model, string? agentVersion, CancellationToken ct);

        /// <summary>آپدیت Heartbeat برای دستگاه (Alive Signal)</summary>
        Task UpdateHeartbeatAsync(string ip, CancellationToken ct);

        /// <summary>غیرفعال‌سازی منطقی دستگاه</summary>
        Task DeactivateAsync(Guid deviceId, CancellationToken ct);
        Task<IReadOnlyList<GetDevicesViewModel>> GetDevicesAsync(CancellationToken cancellationToken);
        Task<OperationResult> UpdateDevice_LegacyAsync(UpdateDeviceViewModel model, CancellationToken cancellationToken = default);
        Task<PageResultDto<DeviceViewModel>> GetAllDevicesAsync(DeviceFilterViewModel filter, CancellationToken cancellationToken = default);

        Task<byte[]> RequestScreenshotAsync(string deviceIp, CancellationToken ct = default);
        Task<bool> RestartOrShutdownDevice(bool isrestart,string deviceIp, CancellationToken cancellationToken = default);
        Task<DeviceMetricsViewModel>GetMetricsAsync(string deviceIp, CancellationToken cancellationToken = default);
        Task<byte[]?> RequestJournalAsync(string deviceIp, DateTime startLocal, DateTime endLocal, CancellationToken ct = default);
        Task<bool> RequestResetCdmAsync(string deviceIp, CancellationToken ct = default);
        Task<bool> RequestResetPtrAsync(string deviceIp, CancellationToken ct = default);
        Task<bool> RequestResetIdcAsync(string deviceIp, CancellationToken ct = default);
        Task<bool> RequestUploadFileAsync(string deviceIp, IBrowserFile file, CancellationToken ct = default);
        Task<DeviceMetricsViewModel?> RequestGetMetricsAsync(string deviceIp, CancellationToken ct = default);
        Task<HomeChartsViewModel> GetVisualizeHome(string userName, CancellationToken ct = default);
        Task<List<BranchErrorAggDto>> GetTop10BranchesByErrorsAsync(CancellationToken ct = default);
        Task<int> CheckHeartBeat( CancellationToken cancellationToken = default);
        Task<string> ExportExcelAsync(DeviceFilterViewModel filter,CancellationToken cancellation=default);
    }
}
