using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class DeviceSupply : BaseEntity
    {
        public Guid DeviceModuleId { get; set; }
        public DeviceModule Module { get; set; }

        public SupplyType Type { get; set; }
        public SupplyStatus Status { get; set; }
        public int? LevelPercent { get; set; } // 0..100
        public int? Count { get; set; }
        private DeviceSupply() { }

        // ------------------- Factory Method -------------------
        public static DeviceSupply Create(
            Guid moduleId,
            SupplyType type,
            SupplyStatus status,
            int? levelPercent,
            int? count)
        {
            if (moduleId == Guid.Empty)
                throw new ArgumentException("شناسه ماژول معتبر نیست.");

            if (count.HasValue && count.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "تعداد موجود نمی‌تواند منفی باشد.");

            if (levelPercent.HasValue && (levelPercent.Value < 0 || levelPercent.Value > 100))
                throw new ArgumentOutOfRangeException(nameof(levelPercent), "درصد سطح باید بین ۰ تا ۱۰۰ باشد.");

            return new DeviceSupply
            {
                DeviceModuleId = moduleId,
                Type = type,
                Status = status,
                LevelPercent = levelPercent,
                Count = count
            };
        }

        // ------------------- Business Method -------------------
        /// <summary>
        /// به‌روزرسانی موجودی مصرفی
        /// </summary>
        public void Update(SupplyStatus status, int? levelPercent, int? count)
        {
            Status = status;
            LevelPercent = levelPercent;
            Count = count;
        }
    }
}
