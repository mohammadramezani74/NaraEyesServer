using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class DeviceModuleStatus : BaseEntity
    {
        public Guid DeviceModuleId { get; set; }
        public DeviceModule Module { get; set; }
        public HealthStatus Status { get; set; }
        public string StateJson { get; set; } // key-values from XFS/Agent
        public int Severity { get; set; } // 0=info,1=warn,2=error (for sorting)
        private DeviceModuleStatus() { }

        // ------------------- Factory Method -------------------
        public static DeviceModuleStatus Create(
            Guid moduleId,
            HealthStatus status,
            string stateJson,
            int severity)
        {
            if (moduleId == Guid.Empty)
                throw new ArgumentException("شناسه ماژول معتبر نیست.");

            if (string.IsNullOrWhiteSpace(stateJson))
                stateJson = "{}";

            if (severity < 0 || severity > 2)
                throw new ArgumentOutOfRangeException(nameof(severity), "شدت باید بین ۰ تا ۲ باشد.");

            return new DeviceModuleStatus
            {
                DeviceModuleId = moduleId,
                Status = status,
                StateJson = stateJson,
                Severity = severity
            };
        }

        // ------------------- Business Method -------------------
        /// <summary>
        /// به‌روزرسانی وضعیت فعلی ماژول
        /// </summary>
        public void Update(HealthStatus status, string stateJson, int severity)
        {
            Status = status;
            StateJson = string.IsNullOrWhiteSpace(stateJson) ? "{}" : stateJson;
            Severity = (severity < 0 || severity > 2) ? 0 : severity;
            ModifiedDate = DateTime.Now;
        }
    }
}
