using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public sealed class ScreenshotCommandPayload
    {
        public Guid CommandId { get; set; }                
    }
    public sealed class ScreenshotAckPayload
    {
        public Guid CommandId { get; set; }                // همون OutBoxDeviceMessage.Id
        public string ContentType { get; set; } = "image/png";
        public string DataBase64 { get; set; } = string.Empty;
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
    public sealed class JournalAckPayload
    {
        public Guid CommandId { get; set; }
        public string? DataBase64 { get; set; }       // zip یا تک فایل
        public string? ContentType { get; set; }      // اختیاری: "application/zip" یا "text/plain"
        public string? FileName { get; set; }         // اختیاری: نام خروجی (مثلاً journal_20250714-20250715.zip)
        public string? Message { get; set; }          // اختیاری: توضیح (مثلاً "No files found")
    }

}
