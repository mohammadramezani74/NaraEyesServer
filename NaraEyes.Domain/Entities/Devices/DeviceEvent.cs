using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Identity;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class DeviceEvent : BaseEntity
    {
        public Guid DeviceId { get;internal set; }
        public Device Device { get; internal set; }

        /// <summary>زمان دقیق رخداد (UTC)</summary>
        public DateTime EventTime { get; private set; }

        /// <summary>شدت رویداد (Info, Warning, Error, Critical)</summary>
        public EventSeverity Severity { get; private set; }

        /// <summary>ماژول منبع رویداد (CDM, IDC, PTR, PIN, SIU...)</summary>
        public DeviceModuleType Module { get; private set; }

        /// <summary>کد فنی خطا/رویداد (مثلاً CDM-LOWCASH یا WFS_ERR_CASHUNITFULL)</summary>
        public string Code { get; private set; }

        /// <summary>پیام قابل‌خواندن برای اپراتور</summary>
        public string Message { get; private set; }

        /// <summary>داده خام (مثلاً WFSRESULT, JSON Payload)</summary>
        public string PayloadJson { get; private set; }

        /// <summary>آیا رویداد Ack یا Resolve شده؟</summary>
        public bool Acknowledged { get; private set; }
        public DateTime? AcknowledgedAt { get; private set; }
        public User? AcknowledgedBy { get; private set; }
        public Guid? AcknowledgedById { get; private set; }


        // ========= Factory =========

        /// <summary>
        /// می‌سازد یک رویداد جدید برای دستگاه/ماژول مشخص. همه ورودی‌ها نرمالایز می‌شوند.
        /// </summary>
        public static DeviceEvent Create(
            Guid deviceId,
            DeviceModuleType module,
            EventSeverity severity,
            string code,
            string message,
            string? payloadJson
           )
        {
            return new DeviceEvent
            {
                DeviceId = EnsureDeviceId(deviceId),
                Module = module,
                Severity = severity,
                Code =code,
                Message = message,
                PayloadJson = NormalizeJson(payloadJson),
                EventTime = DateTime.Now,
                Acknowledged = false,
                AcknowledgedAt = null,
                AcknowledgedBy = null,
                AcknowledgedById = null,
                CreateDate = DateTime.Now,
                Deleted = false,
                
            };
        }
        public static DeviceEvent CreateJournal(
    Guid deviceId,
    string code,
    string message,
    string? payloadJson
   )
        {
            return new DeviceEvent
            {
                DeviceId = EnsureDeviceId(deviceId),
                Module = DeviceModuleType.Journal,
                Severity = EventSeverity.Info,
                Code = code,
                Message = message,
                PayloadJson = NormalizeJson(payloadJson),
                EventTime = DateTime.Now,
                Acknowledged = false,
                AcknowledgedAt = null,
                AcknowledgedBy = null,
                AcknowledgedById = null,
                CreateDate = DateTime.Now,
                Deleted = false,

            };
        }

        /// <summary>
        /// فکتوری راحت‌تر: زمان رو الان (UTC) می‌گذارد.
        /// </summary>
        public static DeviceEvent CreateNow(
            Guid deviceId,
            DeviceModuleType module,
            EventSeverity severity,
            string code,
            string message,
            string? payloadJson = null)
            => Create(deviceId, module, severity, code, message, payloadJson);

        // ========= Behavior =========

        /// <summary>
        /// اَک‌کردن رویداد (idempotent). اگر قبلاً Ack شده باشد، فقط اطلاعات را به‌روز می‌کند.
        /// </summary>
        public void Acknowledge(Guid userId, User? user = null)
        {
            Acknowledged = true;
            AcknowledgedAt = DateTime.Now;
            AcknowledgedById = userId;
            AcknowledgedBy = user; 
        }

        /// <summary>
        /// اَک‌کردن با شیء کاربر (راحت برای جایی که کاربر را داری).
        /// </summary>
        public void Acknowledge(User user, DateTime nowUtc)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            Acknowledge(user.Id, user);
        }

        /// <summary>
        /// برگرداندن به حالت بدون اَک (در صورت اشتباه). 
        /// </summary>
        public void Unacknowledge()
        {
            Acknowledged = false;
            AcknowledgedAt = null;
            AcknowledgedById = null;
            AcknowledgedBy = null;
        }

        /// <summary>
        /// تغییر شدت (در صورت reclassification) – ساده و مستقیم.
        /// </summary>
        public void SetSeverity(EventSeverity severity) => Severity = severity;

        /// <summary>
        /// به‌روزرسانی پیام قابل خواندن برای اپراتور (Trim و محدودیت طول).
        /// </summary>
        public void UpdateMessage(string message) => Message = NormalizeMessage(message);

        /// <summary>
        /// به‌روزرسانی/جایگزینی کل payload (JSON).
        /// </summary>
        public void UpdatePayload(string? payloadJson) => PayloadJson = NormalizeJson(payloadJson);

        /// <summary>
        /// ادغام داده‌های جدید در payload (Override ساده؛ اگر merge لازم داری، اینجا JSON-merge بنویس).
        /// </summary>
        public void MergePayload(object data)
        {
            // فعلاً: جایگزینی کامل
            PayloadJson = JsonSerializer.Serialize(data);
        }

        /// <summary>
        /// اگر نیاز باشد EventTime را به‌روز کنی (مثلاً هنگام normalize ورودی‌های دیررس).
        /// </summary>
        public void SetEventTime(DateTime eventTimeUtc) => EventTime =DateTime.Now;

        // ========= Guards / Normalizers =========

        private static Guid EnsureDeviceId(Guid id)
            => id != Guid.Empty ? id : throw new ArgumentException("DeviceId is required.");



        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) message = "—";
            message = message.Trim();
            if (message.Length > 1000) message = message[..1000];
            return message;
        }

        private static string NormalizeJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "{}";
            return json.Trim();
        }

    }

  

}
