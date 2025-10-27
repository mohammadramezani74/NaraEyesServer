using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Base
{
    public class InBoxDeviceMessage : BaseEntity
    {
        public string DeviceIp { get; set; } = string.Empty;
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
        public MessageType MessageType { get; set; }  
        public string Payload { get; set; } = string.Empty;
    }
}
