using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Metrics
{
    public class DeviceMetricsViewModel
    {
        public string Ip { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? Branch { get; set; }
        public string? InstallationDate { get; set; }
        public string? AgentVersion { get; set; }
        public string? LastHeartBeat { get; set; }
        public string? OperatorName { get; set; }
        public string? OperatorMobile { get; set; }
        public string? DeviceSerial { get; set; }
        public string? DeviceModel { get; set; }
        public string? TotalRam { get; set; }
        public string? CpuModel { get; set; }
        public ChartMetrics? CpuUsage { get; set; }
        public ChartMetrics? RamUsage{ get; set; }
        public ChartMetrics? DiskUsage { get; set; }
    }
    public class ChartMetrics
    {
        public double usage { get; set; }
        public double free => 100 - usage;

    }
}
