using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Alarms;
using NaraEyes.Application.Contracts.Interfaces.Hardware;
using NaraEyes.Application.Contracts.Models.Alarms;
using NaraEyes.Application.Contracts.Models.Hardware;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Hardware
{
    public class HardwareProfileService : IHardwareProfileService
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IAlarmService _alarms;
        private readonly ILogger<HardwareProfileService> _logger;

        /// <summary>
        /// اختلاف کمتر از این مقدار در حافظه نادیده گرفته می‌شود.
        ///
        /// بعضی بایوس‌ها بخش کوچکی از رم را برای گرافیک یکپارچه رزرو
        /// می‌کنند و مقدار گزارش‌شده بعد از آپدیت بایوس چند مگابایت
        /// جابه‌جا می‌شود. بدون این حاشیه، یک آپدیت بایوس می‌تواند بدون
        /// هیچ تعویضی آلارم «کاهش حافظه» بزند.
        /// </summary>
        private const int RamToleranceMb = 64;

        /// <summary>
        /// همین حاشیه برای دیسک — ظرفیت گزارش‌شده بسته به درایور کمی
        /// فرق می‌کند.
        /// </summary>
        private const long DiskToleranceBytes = 2L * 1024 * 1024 * 1024;

        public HardwareProfileService(
            IApplicationUnitOfWork uow,
            IAlarmService alarms,
            ILogger<HardwareProfileService> logger)
        {
            _uow = uow;
            _alarms = alarms;
            _logger = logger;
        }

        public async Task ProcessAsync(
            string deviceIp, HardwareProfilePayload p, CancellationToken ct = default)
        {
            // ناقص را دور بینداز. ایجنت هم نباید بفرستد، ولی سرور هم
            // اعتماد نمی‌کند — نبود داده هرگز نباید نتیجه‌ی منفی بدهد.
            if (p is null || !p.IsComplete) return;
            if (p.RamTotalMb <= 0 || p.CpuCores <= 0 || p.DiskSizeBytes <= 0) return;

            var device = await _uow.Devices
                .FirstOrDefaultAsync(d => d.Ip == deviceIp, ct);

            if (device is null) return;

            var now = DateTime.Now;

            var ramSignature = BuildRamSignature(p);
            var ramJson = JsonSerializer.Serialize(p.RamModules);
            var rawJson = JsonSerializer.Serialize(p);

            var profile = await _uow.DeviceHardwareProfiles
                .FirstOrDefaultAsync(x => x.DeviceId == device.Id, ct);

            // ---------------------------------------------------------
            //  اولین بار — مبنا
            // ---------------------------------------------------------
            if (profile is null)
            {
                profile = DeviceHardwareProfile.Create(device.Id, now);
                ApplyPayload(profile, p, ramSignature, ramJson, rawJson, now, markChanged: false);

                _uow.DeviceHardwareProfiles.Add(profile);
                await _uow.SaveChangesAsync(ct);

                // Info است، نه Warning. اولین ثبت «تغییر» نیست.
                // اگر این Warning بود، روز اول استقرار ۳۰۰ آلارم می‌گرفتی
                // و کاربر از همان ابتدا یاد می‌گرفت زنگوله را نادیده بگیرد.
                await _alarms.RaiseAsync(
                    device.Id,
                    DeviceModuleType.System,
                    EventSeverity.Info,
                    AlarmCodes.HardwareBaseline,
                    $"پروفایل سخت‌افزار ثبت شد — {Describe(p)}",
                    rawJson,
                    suppressWindow: TimeSpan.FromDays(365),
                    ct: ct);

                return;
            }

            // ---------------------------------------------------------
            //  مقایسه
            // ---------------------------------------------------------
            var changes = Compare(profile, p, ramSignature);

            if (changes.Count == 0)
            {
                profile.Touch(now);
                await _uow.SaveChangesAsync(ct);
                return;
            }

            foreach (var c in changes)
            {
                var change = DeviceHardwareChange.Create(
                    device.Id, c.Component, c.Kind, c.OldValue, c.NewValue, c.Description, now);

                _uow.DeviceHardwareChanges.Add(change);
            }

            ApplyPayload(profile, p, ramSignature, ramJson, rawJson, now, markChanged: true);
            await _uow.SaveChangesAsync(ct);

            // ---------------------------------------------------------
            //  آلارم
            // ---------------------------------------------------------
            foreach (var c in changes)
            {
                var severity = SeverityFor(c.Kind);
                var code = c.Kind == HardwareChangeKind.Downgrade
                    ? AlarmCodes.HardwareDowngrade
                    : AlarmCodes.HardwareChanged;

                await _alarms.RaiseAsync(
                    device.Id,
                    DeviceModuleType.System,
                    severity,
                    code,
                    c.Description,
                    JsonSerializer.Serialize(new { c.Component, c.OldValue, c.NewValue }),

                    // پنجره‌ی کوتاه عمدی است. تغییر سخت‌افزار نادر است و
                    // هر بار باید دیده شود. اگر شش ساعت پیش‌فرض را
                    // بگذاریم و کارشناس در یک نوبت دو قطعه عوض کند،
                    // ممکن است دومی سرکوب شود — ولی چون کد آلارم شامل
                    // نوع قطعه نیست، این ریسک واقعی است.
                    suppressWindow: TimeSpan.FromMinutes(30),
                    ct: ct);

                _logger.LogWarning(
                    "Hardware change on {Ip}: {Desc}", deviceIp, c.Description);
            }
        }

        // =============================================================
        //  مقایسه
        // =============================================================

        private sealed record Change(
            HardwareComponent Component,
            HardwareChangeKind Kind,
            string? OldValue,
            string? NewValue,
            string Description);

        private static List<Change> Compare(
            DeviceHardwareProfile old, HardwareProfilePayload now, string newRamSignature)
        {
            var list = new List<Change>();

            // ---------- حافظه ----------
            int ramDiff = now.RamTotalMb - old.RamTotalMb;

            if (ramDiff < -RamToleranceMb)
            {
                list.Add(new Change(
                    HardwareComponent.Ram, HardwareChangeKind.Downgrade,
                    $"{old.RamTotalMb} MB", $"{now.RamTotalMb} MB",
                    $"کاهش حافظه: از {Gb(old.RamTotalMb)} به {Gb(now.RamTotalMb)}"));
            }
            else if (ramDiff > RamToleranceMb)
            {
                list.Add(new Change(
                    HardwareComponent.Ram, HardwareChangeKind.Upgrade,
                    $"{old.RamTotalMb} MB", $"{now.RamTotalMb} MB",
                    $"افزایش حافظه: از {Gb(old.RamTotalMb)} به {Gb(now.RamTotalMb)}"));
            }
            else if (!string.IsNullOrEmpty(old.RamSignature)
                  && !string.IsNullOrEmpty(newRamSignature)
                  && old.RamSignature != newRamSignature)
            {
                // ظرفیت یکسان ولی ماژول‌ها فرق دارند.
                //
                // ⚠️ این فقط وقتی تشخیص داده می‌شود که سریال یا پارت‌نامبر
                // رم در دسترس باشد. روی بردهای صنعتی اغلب نیست — در آن
                // حالت تعویض رم ۴ گیگ با ۴ گیگ دیگر **دیده نمی‌شود**.
                list.Add(new Change(
                    HardwareComponent.Ram, HardwareChangeKind.Replaced,
                    old.RamSignature, newRamSignature,
                    $"تعویض ماژول حافظه — ظرفیت همان {Gb(now.RamTotalMb)} است"));
            }

            // ---------- پردازنده ----------
            if (now.CpuCores < old.CpuCores)
            {
                list.Add(new Change(
                    HardwareComponent.Cpu, HardwareChangeKind.Downgrade,
                    $"{old.CpuName} — {old.CpuCores} هسته",
                    $"{now.CpuName} — {now.CpuCores} هسته",
                    $"کاهش تعداد هسته‌ی پردازنده: از {old.CpuCores} به {now.CpuCores}"));
            }
            else if (now.CpuMaxClockMhz > 0 && old.CpuMaxClockMhz > 0
                  && now.CpuMaxClockMhz < old.CpuMaxClockMhz - 50)
            {
                list.Add(new Change(
                    HardwareComponent.Cpu, HardwareChangeKind.Downgrade,
                    $"{old.CpuName} — {old.CpuMaxClockMhz} MHz",
                    $"{now.CpuName} — {now.CpuMaxClockMhz} MHz",
                    $"کاهش فرکانس پردازنده: از {old.CpuMaxClockMhz} به {now.CpuMaxClockMhz} مگاهرتز"));
            }
            else if (!SameText(old.CpuName, now.CpuName))
            {
                // عمداً مدل‌ها رتبه‌بندی نمی‌شوند.
                //
                // «i5-3570 بهتر است یا i3-4130؟» چاه بی‌انتهاست و هر
                // جدولی که بنویسیم با نسل بعدی پردازنده‌ها غلط می‌شود.
                // تعداد هسته و فرکانس عددی‌اند و برای تشخیص تنزل کافی.
                // تغییر نام مدل جدا و با شدت پایین‌تر ثبت می‌شود.
                list.Add(new Change(
                    HardwareComponent.Cpu, HardwareChangeKind.Replaced,
                    old.CpuName, now.CpuName,
                    $"تعویض پردازنده: از «{old.CpuName}» به «{now.CpuName}»"));
            }

            // ---------- دیسک ----------
            long diskDiff = now.DiskSizeBytes - old.DiskSizeBytes;

            if (diskDiff < -DiskToleranceBytes)
            {
                list.Add(new Change(
                    HardwareComponent.Disk, HardwareChangeKind.Downgrade,
                    GbDisk(old.DiskSizeBytes), GbDisk(now.DiskSizeBytes),
                    $"کاهش ظرفیت هارد: از {GbDisk(old.DiskSizeBytes)} به {GbDisk(now.DiskSizeBytes)}"));
            }
            else if (diskDiff > DiskToleranceBytes)
            {
                list.Add(new Change(
                    HardwareComponent.Disk, HardwareChangeKind.Upgrade,
                    GbDisk(old.DiskSizeBytes), GbDisk(now.DiskSizeBytes),
                    $"افزایش ظرفیت هارد: از {GbDisk(old.DiskSizeBytes)} به {GbDisk(now.DiskSizeBytes)}"));
            }
            else if (BothPresent(old.DiskSerial, now.DiskSerial)
                  && !SameText(old.DiskSerial, now.DiskSerial))
            {
                list.Add(new Change(
                    HardwareComponent.Disk, HardwareChangeKind.Replaced,
                    $"{old.DiskModel} ({old.DiskSerial})",
                    $"{now.DiskModel} ({now.DiskSerial})",
                    $"تعویض هارد — ظرفیت همان {GbDisk(now.DiskSizeBytes)} است، سریال متفاوت"));
            }

            // ---------- مادربرد ----------
            //
            // تعویض مادربرد یعنی عملاً کل کامپیوتر عوض شده، پس همیشه
            // بحرانی است حتی اگر مدل یکسان باشد.
            if (BothPresent(old.BoardSerial, now.BoardSerial)
             && !SameText(old.BoardSerial, now.BoardSerial))
            {
                list.Add(new Change(
                    HardwareComponent.Motherboard, HardwareChangeKind.Downgrade,
                    $"{old.BoardProduct} ({old.BoardSerial})",
                    $"{now.BoardProduct} ({now.BoardSerial})",
                    $"تعویض مادربرد — سریال از «{old.BoardSerial}» به «{now.BoardSerial}»"));
            }
            else if (BothPresent(old.BoardProduct, now.BoardProduct)
                  && !SameText(old.BoardProduct, now.BoardProduct))
            {
                list.Add(new Change(
                    HardwareComponent.Motherboard, HardwareChangeKind.Downgrade,
                    old.BoardProduct, now.BoardProduct,
                    $"تعویض مادربرد: از «{old.BoardProduct}» به «{now.BoardProduct}»"));
            }

            return list;
        }

        private static EventSeverity SeverityFor(HardwareChangeKind kind)
        {
            if (kind == HardwareChangeKind.Downgrade) return EventSeverity.Critical;
            if (kind == HardwareChangeKind.Replaced) return EventSeverity.Warning;
            return EventSeverity.Info;
        }

        // =============================================================
        //  کمکی
        // =============================================================

        private static void ApplyPayload(
            DeviceHardwareProfile profile, HardwareProfilePayload p,
            string ramSignature, string ramJson, string rawJson,
            DateTime at, bool markChanged)
        {
            profile.Apply(
                p.RamTotalMb, ramSignature, ramJson,
                p.CpuName, p.CpuCores, p.CpuMaxClockMhz, p.CpuId,
                p.DiskModel, p.DiskSizeBytes, p.DiskSerial,
                p.BoardManufacturer, p.BoardProduct, p.BoardSerial, p.BiosVersion,
                rawJson, at, markChanged);
        }

        /// <summary>
        /// امضای ماژول‌های حافظه — مرتب‌شده تا ترتیب گزارش WMI روی نتیجه
        /// اثر نگذارد.
        ///
        /// اگر هیچ ماژولی سریال یا پارت‌نامبر نداشته باشد، رشته‌ی خالی
        /// برمی‌گردد و مقایسه‌ی هم‌ظرفیت انجام نمی‌شود — چون امضایی که
        /// فقط از ظرفیت ساخته شده باشد هیچ اطلاعات جدیدی ندارد و فقط
        /// آلارم کاذب می‌سازد.
        /// </summary>
        private static string BuildRamSignature(HardwareProfilePayload p)
        {
            if (p.RamModules is null || p.RamModules.Count == 0) return "";

            bool anyIdentity = p.RamModules.Any(m =>
                !string.IsNullOrWhiteSpace(m.SerialNumber)
                || !string.IsNullOrWhiteSpace(m.PartNumber));

            if (!anyIdentity) return "";

            var parts = p.RamModules
                .Select(m => $"{m.CapacityMb}|{m.PartNumber}|{m.SerialNumber}")
                .OrderBy(s => s, StringComparer.Ordinal);

            var sb = new StringBuilder();
            foreach (var s in parts)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(s);
            }

            return sb.ToString();
        }

        private static bool BothPresent(string? a, string? b)
            => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b);

        private static bool SameText(string? a, string? b)
            => string.Equals((a ?? "").Trim(), (b ?? "").Trim(),
                             StringComparison.OrdinalIgnoreCase);

        private static string Gb(int mb)
            => (mb / 1024.0).ToString("0.##") + " گیگابایت";

        private static string GbDisk(long bytes)
            => (bytes / 1000.0 / 1000.0 / 1000.0).ToString("0") + " گیگابایت";

        private static string Describe(HardwareProfilePayload p)
            => $"حافظه {Gb(p.RamTotalMb)}، پردازنده {p.CpuName}، هارد {GbDisk(p.DiskSizeBytes)}";
    }
}

