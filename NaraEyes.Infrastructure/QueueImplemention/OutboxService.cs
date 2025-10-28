using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Bulkoperations;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using NaraEyes.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.QueueImplemention
{
    internal class OutboxService : IOutboxService
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IDeviceSignalHub _signals;

        public OutboxService(IApplicationUnitOfWork uow, IDeviceSignalHub signals)
        {
            _uow = uow;
            _signals = signals;
        }

        public async Task EnqueueCommandAsync(OutBoxDeviceMessage command, CancellationToken ct)
        {
            await _uow.OutBoxDeviceMessages.AddAsync(command, ct);
            await _uow.SaveChangesAsync(ct);
           await _signals.Pulse(command.DeviceIp);
        }

        public async Task<List<OutBoxDeviceMessage>> GetPendingCommandsAsync(string deviceIp, CancellationToken ct)
        { string groupedIp = "255.255.255.0";
            var list = new List<OutBoxDeviceMessage>();
            var ordinaryMessage= await _uow.OutBoxDeviceMessages
     .Where(m => m.DeviceIp == deviceIp && !m.Processed&&m.DeviceIp!= groupedIp)
     .OrderBy(m => m.CreateDate)
     .ToListAsync(ct);
            list.AddRange(ordinaryMessage);

            var outBoxMessageGroup = await _uow.OutBoxDeviceMessages
                .Include(x => x.Campaign.Targets)
                .Where(x => !x.Processed && x.DeviceIp == groupedIp
                &&x.Campaign.Status== OperationStatus.Queued
                && x.Campaign.Targets.Any(x => x.DeviceIp == deviceIp && x.IsProccessed == false))
                .ToListAsync(ct);

            foreach (var messagebox in outBoxMessageGroup)
            {
               var type = messagebox.Campaign;
                if (type == null)
                {
                    continue;
                }
                var newinstruction = new OutBoxDeviceMessage
                {
                    Id= messagebox.Id,
                    CommandType= type.OperationType==OperationType.FileSend?CommandType.UploadGroupFile: CommandType.ResetGroup,
                    Payload=type.ManifestJson

                };
                list.Add(newinstruction);
            }
            return list;


        }

        public async Task MarkAutoJournalProccessor(string deviceIp, byte[]? file, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(deviceIp))
                return;

            var device = await _uow.Devices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Ip == deviceIp, ct);
            if (device == null)
                return;


            var pc = new PersianCalendar();
            var now = DateTime.Now.AddDays(-1);
            int y = pc.GetYear(now);
            int m = pc.GetMonth(now);
            int d = pc.GetDayOfMonth(now);

            // نام ماه فارسی
            string[] faMonths = {
        "فروردین","اردیبهشت","خرداد","تیر","مرداد","شهریور",
        "مهر","آبان","آذر","دی","بهمن","اسفند"
    };
            string faMonth = (m >= 1 && m <= 12) ? faMonths[m - 1] : "نامشخص";

            // ساخت مسیرها
            string root = @"C:\JournalBackup";
            string yearFolder = Path.Combine(root, y.ToString(CultureInfo.InvariantCulture));
            string monthFolder = Path.Combine(yearFolder, faMonth);
            string dayFolder = Path.Combine(monthFolder, $"{y:0000}-{m:00}-{d:00}");

            // نام فایل امن بر اساس آی‌پی (نقطه/دو‌نقطه و کاراکترهای ممنوع → '-')
            string ipFileSafe = deviceIp.Replace('.', '-').Replace(':', '-');
            ipFileSafe = Regex.Replace(ipFileSafe, @"[^0-9A-Za-z_-]", "-");
            string savedPath = null;

            try
            {
                // فقط اگر قرار است چیزی ذخیره شود، پوشه‌ها را بساز
                Directory.CreateDirectory(dayFolder);

                if (file != null && file.Length > 0)
                {
                    // فرض بر ZIP بودن داده ارسالی
                    string fileName = ipFileSafe + ".zip";
                    string fullPath = Path.Combine(dayFolder, fileName);

                    // بازنویسی ایمن
                    await File.WriteAllBytesAsync(fullPath, file, ct);
                    savedPath = fullPath;
                }
                else
                {
                    // فایل نداشتیم؛ payload مسیر فولدر روز
                    savedPath = dayFolder;
                }

                string serverResponse = (file != null && file.Length > 0)
                    ? $"فایل ژورنال دستگاه در تاریخ {DateTime.Now.ToFarsiFull()} روی سرور با موفقیت ذخیره شد!"
                    : $"برای تاریخ {DateTime.Now.ToFarsiFull()} فایلی ارسال نشد، مسیر روز ساخته شد.";

                // ایونت با payload = مسیر ذخیره (فایل یا فولدر روز)
                var newEvent = DeviceEvent.CreateJournal(
                    device.Id,
                    "1",
                    serverResponse,
                    payloadJson: JsonSerializer.Serialize(savedPath)
                );

                await _uow.DeviceEvents.AddAsync(newEvent, ct);
                await _uow.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                var failMsg = $"خطا در ذخیره ژورنال: {ex.Message}";
                var errEvent = DeviceEvent.CreateJournal(
                    device.Id,
                    "0",
                    failMsg,
                    payloadJson: JsonSerializer.Serialize(savedPath ?? dayFolder)
                );
                await _uow.DeviceEvents.AddAsync(errEvent, ct);
                await _uow.SaveChangesAsync(ct);
            }

        }

        public async Task MarkCommandAsFailedAsync(Guid commandId,string?ip, CancellationToken ct)
        {
            var msg = await _uow.OutBoxDeviceMessages.Include(x=>x.Campaign.Targets)
             .FirstOrDefaultAsync(m => m.Id == commandId, ct);
            if (msg.Campaign != null)
            {
                var target = msg.Campaign.Targets.FirstOrDefault(x => x.DeviceIp == ip);
                    if (target != null)
                {
                    target.ProccessdAt();
                    target.IsSuccess = false;
                }
                if (msg.Campaign.Targets.All(x => x.IsProccessed))
                {
                    msg.Campaign.Status = OperationStatus.Completed;
                    msg.Processed = true;
                }
                await _uow.SaveChangesAsync();
            }
          else if (msg != null)
            {
                msg.Deleted = true;
                msg.Processed = true;
                msg.ProcessedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
        }

        public async Task MarkCommandAsProcessedAsync(Guid commandId, CancellationToken ct)
        {
            var msg = await _uow.OutBoxDeviceMessages
             .FirstOrDefaultAsync(m => m.Id == commandId, ct);

            if (msg != null)
            {
                msg.Processed = true;
                msg.ProcessedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
        }
      public async  Task MarkCommandGroupProcessedAsync(SendGroupInstructionModel? pl, CancellationToken ct)
        {
            var msg = await _uow.OutBoxDeviceMessages.Include(x=>x.Campaign.Targets)
        .FirstOrDefaultAsync(m => m.Id == pl.MessageBoxId, ct);
            if (msg != null) { 
            var target=msg.Campaign.Targets.FirstOrDefault(x=>x.DeviceIp==pl.Ip);
            if (target != null)
            {
                target.ProccessdAt();
                target.IsSuccess = true;
            }
            if (msg.Campaign.Targets.All(x => x.IsProccessed))
            {
                msg.Campaign.Status = OperationStatus.Completed;
                msg.Processed = true;
            }
            if (msg.CreateDate > DateTime.Now.AddMinutes(20))
            {
                msg.Campaign.Status = OperationStatus.Completed;
                msg.Processed = true;
            }
            await _uow.SaveChangesAsync();
            }
        }

        public async Task MarkReportFailedSafeAsync(InBoxDeviceMessage msg, CancellationToken ct)
        {
            if (msg == null) return;

            try
            {
                // 1) استخراج CommandId یا MessageBoxId از payload (case-insensitive)
                Guid cmdId = Guid.Empty;
                if (!string.IsNullOrWhiteSpace(msg.Payload))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(msg.Payload);
                        var root = doc.RootElement;

                        static bool TryReadGuid(JsonElement root, string name, out Guid g)
                        {
                            g = Guid.Empty;
                            if (root.ValueKind != JsonValueKind.Object) return false;

                            // تلاش مستقیم
                            if (root.TryGetProperty(name, out var p))
                            {
                                var s = p.ValueKind == JsonValueKind.String ? p.GetString() : null;
                                return Guid.TryParse(s, out g);
                            }

                            // تلاش case-insensitive ساده
                            foreach (var prop in root.EnumerateObject())
                            {
                                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                                {
                                    var s = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
                                    return Guid.TryParse(s, out g);
                                }
                            }
                            return false;
                        }

                        // ترتیب اولویت: CommandId → MessageBoxId → Id
                        if (!TryReadGuid(root, "CommandId", out cmdId) &&
                            !TryReadGuid(root, "MessageBoxId", out cmdId) &&
                            !TryReadGuid(root, "Id", out cmdId))
                        {
                            cmdId = Guid.Empty;
                        }
                    }
                    catch
                    {
                        // payload ممکن است JSON نباشد؛ در این حالت correlation نداریم
                        cmdId = Guid.Empty;
                    }
                }

                // 2) اگر CommandId معتبری نداریم، چیزی به OutBox دست نزن
                if (cmdId == Guid.Empty)
                    return;

                // 3) تلاش برای پیدا کردن رکورد OutBox (با Targets برای حالت گروهی)
                var ob = await _uow.OutBoxDeviceMessages
                    .Include(x => x.Campaign.Targets)
                    .FirstOrDefaultAsync(x => x.Id == cmdId, ct);

                if (ob == null)
                    return;

                // 4) اگر کمپین دارد => شکست برای تارگت همین دستگاه
                if (ob.Campaign != null)
                {
                    var target = ob.Campaign.Targets?.FirstOrDefault(t => t.DeviceIp == msg.DeviceIp);
                    if (target != null)
                    {
                        target.ProccessdAt();
                        target.IsSuccess = false;
                    }

                    // اگر همه تارگت‌ها پردازش شدند، پیام را تمام کن
                    if (ob.Campaign.Targets != null && ob.Campaign.Targets.All(t => t.IsProccessed))
                    {
                        ob.Campaign.Status = OperationStatus.Completed;
                        ob.Processed = true;
                    }

                    await _uow.SaveChangesAsync(ct);
                    return;
                }

                // 5) حالت تکی: از چرخه خارجش کن (مثل MarkCommandAsFailedAsync شاخهٔ else)
                ob.Deleted = true;                  // برای جمع‌آوری/عدم‌نمایش
                ob.Processed = true;
                ob.ProcessedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
            catch
            {
                // Fail-safe: هرگز اجازه نده این مسیر خودش منبع exception لوپ‌ساز شود
                // می‌توانی اینجا یک لاگ سبک بگذاری اگر logger در دسترس داری.
                // _logger.LogWarning(ex, "MarkReportFailedSafeAsync failed for payload: {pl}", msg.Payload);
            }
        }

    }
}
