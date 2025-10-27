using NaraEyes.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class MetricSnapshot : BaseEntity
    {
        public Guid DeviceId { get;private set; }
        public Device Device { get; private set; }

        public DateTime CapturedAt { get; private set; }

        // 🎛️ منابع سیستمی
        public double? CpuUsage { get; private set; }       // درصد استفاده CPU
        public double? RamUsage { get; private set; }       // درصد استفاده RAM
        public double? DiskUsage { get; private set; }      // درصد استفاده دیسک
        public double? TotalRamGb { get; set; }   // مثال: 2.0، 3.9
        public string? CpuModel { get; set; }       // دمای CPU (درجه سانتی‌گراد)

        // 🌐 شبکه
        public int? NetworkLatencyMs { get; private set; }  // پینگ تا سرور
        public bool PingOk { get; private set; }            // نتیجه پینگ

        // ⚡ وضعیت کلی
        public bool AgentAlive { get; private set; }        // Agent سالمه؟
        public string AgentVersion { get; private set; }    // نسخه Agent

        // 🗄️ رزرو برای متریک‌های اضافه
        public string? ExtraJson { get; private set; }       // اطلاعات خاص دستگاه (مثلاً GPU Temp، UPS status)

        /// <summary>
        /// ساخت Snapshot جدید از متریک‌های دریافتی از Agent
        /// </summary>
        public static MetricSnapshot CreateNew(
            Guid deviceId,
            double? cpuUsage,
            double? ramUsage,
            double? diskUsage,
            double? TotalRamGb,
           string cpuModel,

            int? networkLatencyMs,
            bool pingOk,
            bool agentAlive,
            string agentVersion,
            string? extraJson
           )
        {
            return new MetricSnapshot
            {
                DeviceId = deviceId,
                CapturedAt = DateTime.Now,
                CreateDate = DateTime.Now,
                Deleted=false,
                CpuUsage = NormalizePercent(cpuUsage),
                RamUsage = NormalizePercent(ramUsage),
                DiskUsage = NormalizePercent(diskUsage),
                CpuModel=cpuModel,
                TotalRamGb=TotalRamGb,

                NetworkLatencyMs = NormalizeLatency(networkLatencyMs),
                PingOk = pingOk,

                AgentAlive = agentAlive,
                AgentVersion = NormalizeVersion(agentVersion),

                ExtraJson = NormalizeJson(extraJson)
            };
        }
        public void Update(double? cpuUsage,
            double? ramUsage,
            double? diskUsage,
            int? networkLatencyMs,
            bool pingOk,
            bool agentAlive,
            string agentVersion)
        {
            CpuUsage = NormalizePercent(cpuUsage);
            RamUsage = NormalizePercent(ramUsage);
            DiskUsage = NormalizePercent(diskUsage);
NetworkLatencyMs=networkLatencyMs;
            PingOk=pingOk;
            AgentAlive=agentAlive;
            AgentVersion=NormalizeVersion(agentVersion);
            ModifiedDate = DateTime.Now;
        }

        // ===== Behaviors =====

        /// <summary>
        /// Merge اطلاعات اضافی (ExtraJson) با داده جدید
        /// </summary>
        public void MergeExtra(object extra)
        {
            var newJson = JsonSerializer.Serialize(extra);
            if (string.IsNullOrWhiteSpace(ExtraJson))
            {
                ExtraJson = newJson;
            }
            else
            {
                // ساده: override کل فیلد
                ExtraJson = newJson;
                // اگر بخواهی Merge واقعی باشه باید JSON Patch/Dictionary merge بنویسی
            }
        }

        /// <summary>
        /// بروزرسانی وضعیت پینگ
        /// </summary>
        public void UpdatePingResult(int latencyMs, bool ok)
        {
            NetworkLatencyMs = NormalizeLatency(latencyMs);
            PingOk = ok;
            CapturedAt = DateTime.Now;
        }

        // ===== Normalizers / Guards =====

        private static double? NormalizePercent(double? value)
        {
            if (value is null) return null;
            if (value < 0) return 0;
            if (value > 100) return 100;
            return Math.Round(value.Value, 2);
        }

        private static double? NormalizeTemp(double? temp)
        {
            if (temp is null) return null;
            if (temp < -20) return -20;   
            if (temp > 120) return 120;  
            return Math.Round(temp.Value, 1);
        }

        private static int? NormalizeLatency(int? latency)
        {
            if (latency is null) return null;
            if (latency < 0) return null;
            return latency;
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Agent version required");
            return version.Length <= 50 ? version : version[..50];
        }

        private static string NormalizeJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "{}";
            return json.Trim();
        }
    }
}
