using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.DapperModels
{
    public class DeviceMetricsRow
    {
        public string? Ip { get; set; }
        public string? DeviceModel { get; set; }
        public string? DeviceSerial { get; set; }
        public string? Address { get; set; }
        public int? Code { get; set; }

        public DateTime InstallationDate { get; set; }

        public string? BranchShortName { get; set; }

        public string? OperatorMobile { get; set; }
        public string? OperatorName { get; set; }

        public double? DiskUsage { get; set; }
        public double? CpuUsage { get; set; }
        public double? RamUsage { get; set; }

        public string? AgentVersion { get; set; }
        public string? CpuModel { get; set; }
        public DateTime? MetricsModifiedDate { get; set; }
        public double? TotalRamGb { get; set; }
        public string? OsInfo { get; set; }
        public DateTime AgentTime { get; set; }

    }
}
