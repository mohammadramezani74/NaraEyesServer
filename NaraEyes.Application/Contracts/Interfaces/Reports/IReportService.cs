using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Interfaces.Reports
{
    public  interface IReportService
    {
        Task<PageResultDto<UserActivityReport>>GetUserReports(ReportFilterModel Search,CancellationToken cancellationToken=default);
        Task<string?> UserReportsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default);
        Task<PageResultDto<FileUploadReport>> GetFilesReports(ReportFilterModel Search, CancellationToken cancellationToken = default);
        Task<string?> FileUploadsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default);
        Task<PageResultDto<DeviceRestartReport>> GetRestartReports(ReportFilterModel filter, CancellationToken cancellationToken = default);
        Task<string?> GetRestartReportsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default);
    }
}
