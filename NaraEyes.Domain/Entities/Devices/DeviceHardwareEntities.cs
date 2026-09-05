using NaraEyes.Domain.Common;
using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Domain.Entities.Devices
{
    /// <summary>
    /// آخرین پروفایل سخت‌افزار شناخته‌شده‌ی یک دستگاه — یک ردیف به‌ازای
    /// هر دستگاه، در جا به‌روز می‌شود.
    ///
    /// ⚠️ چرا روی سرور و نه روی خود دستگاه:
    ///
    /// اگر مبنا را در فایلی روی ATM ذخیره کنیم، کارشناسی که **هارد را عوض
    /// می‌کند** — یعنی دقیقاً همان کاری که بانک بیشتر از همه نگرانش است —
    /// فایل مبنا را هم با خودش می‌برد. ایجنت روی هارد جدید بالا می‌آید،
    /// مبنایی نمی‌بیند، و مشخصات جدید را به‌عنوان مبنا ثبت می‌کند.
    ///
    /// نتیجه: بدترین مورد ممکن، هیچ آلارمی نمی‌دهد.
    ///
    /// با نگه داشتن مبنا روی سرور و کلید IP، هارد عوض شود یا نشود، مقایسه
    /// انجام می‌شود.
    /// </summary>
    public class DeviceHardwareProfile : BaseEntity
    {
        public Guid DeviceId { get; private set; }
        public Device Device { get; private set; } = null!;

        // ---------- حافظه ----------
        public int RamTotalMb { get; private set; }

        /// <summary>امضای ماژول‌ها برای تشخیص تعویض هم‌ظرفیت</summary>
        public string? RamSignature { get; private set; }
        public string? RamModulesJson { get; private set; }

        // ---------- پردازنده ----------
        public string? CpuName { get; private set; }
        public int CpuCores { get; private set; }
        public int CpuMaxClockMhz { get; private set; }
        public string? CpuId { get; private set; }

        // ---------- دیسک ----------
        public string? DiskModel { get; private set; }
        public long DiskSizeBytes { get; private set; }
        public string? DiskSerial { get; private set; }

        // ---------- مادربرد ----------
        public string? BoardManufacturer { get; private set; }
        public string? BoardProduct { get; private set; }
        public string? BoardSerial { get; private set; }
        public string? BiosVersion { get; private set; }

        // ---------- متادیتا ----------
        public DateTime FirstSeenAt { get; private set; }
        public DateTime LastSeenAt { get; private set; }
        public DateTime? LastChangedAt { get; private set; }

        /// <summary>کل پروفایل خام — برای وقتی که فیلد جدیدی لازم شد</summary>
        public string? RawJson { get; private set; }

        private DeviceHardwareProfile() { }

        public static DeviceHardwareProfile Create(Guid deviceId, DateTime at)
            => new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                FirstSeenAt = at,
                LastSeenAt = at,
                CreateDate = at,
            };

        public void Apply(
            int ramTotalMb, string? ramSignature, string? ramModulesJson,
            string? cpuName, int cpuCores, int cpuMaxClockMhz, string? cpuId,
            string? diskModel, long diskSizeBytes, string? diskSerial,
            string? boardManufacturer, string? boardProduct, string? boardSerial,
            string? biosVersion,
            string? rawJson,
            DateTime at,
            bool markChanged)
        {
            RamTotalMb = ramTotalMb;
            RamSignature = ramSignature;
            RamModulesJson = ramModulesJson;

            CpuName = cpuName;
            CpuCores = cpuCores;
            CpuMaxClockMhz = cpuMaxClockMhz;
            CpuId = cpuId;

            DiskModel = diskModel;
            DiskSizeBytes = diskSizeBytes;
            DiskSerial = diskSerial;

            BoardManufacturer = boardManufacturer;
            BoardProduct = boardProduct;
            BoardSerial = boardSerial;
            BiosVersion = biosVersion;

            RawJson = rawJson;
            LastSeenAt = at;
            ModifiedDate = at;

            if (markChanged) LastChangedAt = at;
        }

        /// <summary>وقتی پروفایل عوض نشده — فقط زنده بودن ثبت می‌شود</summary>
        public void Touch(DateTime at)
        {
            LastSeenAt = at;
            ModifiedDate = at;
        }
    }

    /// <summary>
    /// یک تغییر تشخیص‌داده‌شده. فقط وقتی ردیف ساخته می‌شود که واقعاً چیزی
    /// عوض شده باشد — نه در هر دریافت پروفایل.
    ///
    /// این جدول همان چیزی است که گزارش و خروجی اکسل بعداً رویش ساخته
    /// می‌شود، پس مقدار قبلی و جدید هر دو به‌صورت متنی خوانا ذخیره
    /// می‌شوند نه فقط JSON.
    /// </summary>
    public class DeviceHardwareChange : BaseEntity
    {
        public Guid DeviceId { get; private set; }
        public Device Device { get; private set; } = null!;

        public HardwareComponent Component { get; private set; }
        public HardwareChangeKind Kind { get; private set; }

        /// <summary>مقدار قبلی — خوانا، برای نمایش مستقیم در گزارش</summary>
        public string? OldValue { get; private set; }
        public string? NewValue { get; private set; }

        /// <summary>توضیح فارسی — همان چیزی که در آلارم نمایش داده می‌شود</summary>
        public string Description { get; private set; } = "";

        public DateTime DetectedAt { get; private set; }

        /// <summary>رویداد آلارم متناظر، اگر ثبت شده باشد</summary>
        public Guid? DeviceEventId { get; private set; }

        private DeviceHardwareChange() { }

        public static DeviceHardwareChange Create(
            Guid deviceId,
            HardwareComponent component,
            HardwareChangeKind kind,
            string? oldValue,
            string? newValue,
            string description,
            DateTime at)
            => new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                Component = component,
                Kind = kind,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description,
                DetectedAt = at,
                CreateDate = at,
            };

        public void LinkEvent(Guid eventId) => DeviceEventId = eventId;
    }
}