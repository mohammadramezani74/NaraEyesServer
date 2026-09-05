using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum MessageType
    {
        Heartbeat = 1,       // سیگنال زنده بودن دستگاه (هر ۳۰ ثانیه)
    Metrics = 2,         // اطلاعات CPU, RAM, Disk, Temp
    DeviceEvent = 3,     // رویداد XFS (خطا یا تغییر وضعیت ماژول)
    ScreenshotAck = 4,   // تأیید اجرای دستور اسکرین‌شات
    CommandAck = 5,      // تأیید اجرای سایر دستورات
    ErrorReport = 6,     // گزارش خطای ایجنت یا سیستم
    LogUpload = 7, 
            EJournal=8,
        FileUpload=9,
        Group = 10,
        HardwareProfile = 11
    }
}
