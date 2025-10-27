using NaraEyes.Application.Contracts.Models.Modules.CDM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Metrics
{
    public sealed class DeviceMetricsDto
    {
        public string DeviceIp { get; set; } = null!;
        public DateTime CapturedAtUtc { get; set; }

        // منابع سیستمی
        public double? CpuUsage { get; set; }
        public double? RamUsage { get; set; }
        public double? DiskUsage { get; set; }
        public double? CpuTemp { get; set; }

        // شبکه
        public int? NetworkLatencyMs { get; set; }
        public bool PingOk { get; set; }

        // وضعیت کلی
        public bool AgentAlive { get; set; }
        public string AgentVersion { get; set; }
        public double? TotalRamGb { get; set; }  
        public string? CpuModel { get; set; }
        //وضعیت ماژول دیسپنسر
        public CdmStatusDto? CdmStatus { get; set; }
    }
}
