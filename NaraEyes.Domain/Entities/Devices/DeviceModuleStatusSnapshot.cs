using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class DeviceModuleStatusSnapshot : BaseEntity
    {
        public Guid DeviceModuleId { get; set; }
        public DeviceModule Module { get; set; }
        public DateTime CapturedAt { get; set; }
        public HealthStatus Status { get; set; }
        public string StateJson { get; set; }
        public int Severity { get; set; }
        private DeviceModuleStatusSnapshot() { }


        public static DeviceModuleStatusSnapshot Create(
            Guid moduleId,
            HealthStatus status,
            string stateJson,
            int severity,
            DateTime capturedAt)
        {
            if (moduleId == Guid.Empty)
                throw new ArgumentException("شناسه ماژول معتبر نیست.");

            if (string.IsNullOrWhiteSpace(stateJson))
                stateJson = "{}";

            if (severity < 0 || severity > 2)
                throw new ArgumentOutOfRangeException(nameof(severity), "شدت باید بین ۰ تا ۲ باشد.");

            if (capturedAt == default)
                capturedAt = DateTime.Now;

            return new DeviceModuleStatusSnapshot
            {Id=Guid.NewGuid(),
            CreateDate = capturedAt,
                DeviceModuleId = moduleId,
                Status = status,
                StateJson = stateJson,
                Severity = severity,
                CapturedAt = capturedAt
            };
        }
    }
}
