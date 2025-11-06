using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Metrics
{
    public interface IDeviceMetrics
    {
        /// <summary>
        /// آپدیت یا ثبت متریک ها
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<OperationResult> SubmitOrUpdateMetrics(DeviceMetricsDto command, CancellationToken cancellationToken = default);
        Task<OperationResult> SubmitOrUpdateModulesStatus(DeviceMuduleStatusCommand command, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAgentStatus(string Ip, CancellationToken cancellationToken = default);
    }

}
