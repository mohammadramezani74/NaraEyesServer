using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class DeviceModule : BaseEntity
    {
        public Guid DeviceId { get; set; }
        public Device Device { get; set; }
        public DeviceModuleType Type { get; set; }
        public string Name { get; set; }
        public List<DeviceModuleStatus> DeviceModuleStatuses { get; set; } = new List<DeviceModuleStatus>();
        private DeviceModule() { }

        private DeviceModule(Guid deviceId, DeviceModuleType type, string name)
        {
            DeviceId = deviceId;
            Type = type;
            Name = name;
        }

        public static DeviceModule Create(Guid deviceId, DeviceModuleType type, string name)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("DeviceId نباید خالی باشد.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("نام ماژول نمی‌تواند خالی باشد.");

            return new DeviceModule(deviceId, type, name.Trim());
        }


        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("نام جدید معتبر نیست.");
            Name = newName.Trim();
        }

        public void ChangeType(DeviceModuleType newType)
        {
            Type = newType;
        }
    }
}
