using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Metrics;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Hubs;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using System.Text.Json;


namespace NaraEyes.Application.Services.Metrics
{
    public sealed class DeviceMetricsService : IDeviceMetrics
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IHubContext<DeviceHub> _hubContext;
        /// <summary>
        /// داده‌های ماژول‌های یک دستگاه که یک بار خوانده و بین شش فراخوانی
        /// UpsertModuleStatusAsync به اشتراک گذاشته می‌شوند.
        ///
        /// بدون این، هر ماژول سه تا چهار کوئری جدا می‌زد.
        /// </summary>
        private sealed class ModuleContext
        {
            public Dictionary<Guid, DeviceModuleStatus> Statuses { get; init; } = new();
            public Dictionary<Guid, int> SnapshotCounts { get; init; } = new();
            public Dictionary<Guid, DeviceModuleStatusSnapshot> OldestSnapshots { get; init; } = new();
            public Dictionary<DeviceModuleType, ModuleFaultLog> OpenFaults { get; init; } = new();
        }
        /// <summary>
        /// همه‌ی داده‌ی موردنیاز شش ماژول را در چهار کوئری می‌خواند،
        /// به‌جای بیست‌وچهار کوئری جدا.
        /// </summary>
        private async Task<ModuleContext> PreloadModuleContextAsync(
            Device atm, CancellationToken ct)
        {
            var moduleIds = atm.Modules.Select(m => m.Id).ToList();

            if (moduleIds.Count == 0)
                return new ModuleContext();

            // کوئری ۱ — وضعیت فعلی همه‌ی ماژول‌ها
            var statuses = await _uow.DeviceModuleStatuses
                .Where(x => moduleIds.Contains(x.DeviceModuleId))
                .ToListAsync(ct);

            // کوئری ۲ — تعداد اسنپ‌شات هر ماژول
            var counts = await _uow.DeviceModuleStatusSnapshots
                .Where(s => moduleIds.Contains(s.DeviceModuleId))
                .GroupBy(s => s.DeviceModuleId)
                .Select(g => new { ModuleId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // کوئری ۳ — قدیمی‌ترین اسنپ‌شات هر ماژول (فقط آن‌هایی که به سقف رسیده‌اند)
            var fullModuleIds = counts
                .Where(c => c.Count >= MaxSnapshots)
                .Select(c => c.ModuleId)
                .ToList();

            var oldest = new Dictionary<Guid, DeviceModuleStatusSnapshot>();

            if (fullModuleIds.Count > 0)
            {
                var snaps = await _uow.DeviceModuleStatusSnapshots
                    .Where(s => fullModuleIds.Contains(s.DeviceModuleId))
                    .GroupBy(s => s.DeviceModuleId)
                    .Select(g => g.OrderBy(s => s.CreateDate).First())
                    .ToListAsync(ct);

                oldest = snaps.ToDictionary(s => s.DeviceModuleId);
            }

            // کوئری ۴ — بازه‌های خرابی باز
            var openFaults = await _uow.ModuleFaultLogs
                .Where(f => f.DeviceId == atm.Id && f.ResolvedAt == null)
                .ToListAsync(ct);

            return new ModuleContext
            {
                Statuses = statuses.ToDictionary(x => x.DeviceModuleId),
                SnapshotCounts = counts.ToDictionary(c => c.ModuleId, c => c.Count),
                OldestSnapshots = oldest,
                OpenFaults = openFaults
                    .GroupBy(f => f.Module)
                    .ToDictionary(g => g.Key, g => g.First()),
            };
        }
        public DeviceMetricsService(IApplicationUnitOfWork uow,IHubContext<DeviceHub> hubContext)
        {
            _hubContext = hubContext;
            _uow = uow;
        }

        public async Task<OperationResult> SubmitOrUpdateMetrics(DeviceMetricsDto command, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var atm = await _uow.Devices.Include(c => c.CurrentMetrics)

                                                                                .FirstOrDefaultAsync(x => x.Ip == command.DeviceIp, cancellationToken);
            if (atm == null) { return op.Failed("دستگاهی یافت نشد!!"); }
            if (atm!.CurrentMetrics == null)
            {
                var newMetrics = MetricSnapshot.CreateNew(atm.Id, command.CpuUsage,
                      command.RamUsage, command.DiskUsage, command.TotalRamGb,
                      command.CpuModel, command.NetworkLatencyMs, command.PingOk, command.AgentAlive, command.AgentVersion, null,command.OsFeatures,command.AgentTime);
                atm.CurrentMetricsId = newMetrics.Id;
                _uow.MetricSnapshots.Add(newMetrics);
            }
            else
            {
                atm!.CurrentMetrics.Update(command.CpuUsage, command.RamUsage, command.DiskUsage, command.NetworkLatencyMs
                    , command.PingOk, command.AgentAlive, command.AgentVersion, command.OsFeatures, command.AgentTime);
            }

  
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
        }

        public async Task<OperationResult> SubmitOrUpdateModulesStatus(DeviceMuduleStatusCommand command, CancellationToken cancellationToken = default)
        {
            var haveError = false;
            bool HaveWarning=false;
            DeviceMode mode = DeviceMode.Supervisor;
            var op = new OperationResult();
            const int MaxSnapshots = 10;
            var now = DateTime.Now;

            Device? atm = await _uow.Devices.Include(x => x.Modules)
                .Include(x=>x.CashUnits)
                                     .FirstOrDefaultAsync(x => x.Ip == command.DeviceIp, cancellationToken);
        
            if (atm == null) { return op.Failed("دستگاهی یافت نشد!!"); }
            var atmmode = atm.Mode;
            var mc = await PreloadModuleContextAsync(atm, cancellationToken);
            if (command.CdmStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
    atm,
    DeviceModuleType.Cdm,
    "CashDispenser",
    command.CdmStatus.Device,
    command.CdmStatus,
     mc,
    cancellationToken);
            
            }

            if (command.IdcStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Idc,
                                                              "CardReader",
                                                              command.IdcStatus.Device,
                                                              command.IdcStatus,
                                                               mc,
                                                              cancellationToken);
           
            }

            if (command.ptrStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Ptr,
                                                              "ReceiptPrinter",
                                                              command.ptrStatus.Device,
                                                              command.ptrStatus,
                                                               mc,
                                                              cancellationToken);
            
            }
            if (command.CameraStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Camera,
                                                              "Cameras",
                                                              command.CameraStatus.Device,
                                                              command.CameraStatus,
                                                               mc,
                                                              cancellationToken);
           
            }
            if (command.SiuStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Siu,
                                                              "Sensors",
                                                              command.SiuStatus.Device,
                                                              command.SiuStatus,
                                                               mc,
                                                              cancellationToken);
            
            }
            if (command.PinStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Pin,
                                                              "Encryptor",
                                                              command.PinStatus.Device,

                                                              command.PinStatus,
                                                               mc
                                                               , cancellationToken);
           
            }
            if (command.Cashunit != null)
            {
                var targetCashunit = _uow.CashUnits.Where(x => x.DeviceId == atm.Id).ToList();
                if (targetCashunit?.Count == 0)
                {
                    var type = CashUnitType.Bill;
                 
                        foreach (var cu in command.Cashunit)
                        {
                            var unitstatus = cu.Count < 500 ? CashUnitStatus.Low : CashUnitStatus.Full;
                            if (cu.Count == 0)
                            {
                                unitstatus = CashUnitStatus.Empty;
                            }
                            if (cu.UnitId == "LCU00"|| cu.UnitId == "12345")
                            {
                                type = CashUnitType.Reject;
                                unitstatus = CashUnitStatus.Ok;
                            }
                            var cashUnit = CashUnit.Create(atm.Id, cu.UnitId, cu.currency, Guid.NewGuid().ToString(), cu.Denomination, cu.Init.ToString(), cu.Count.ToString(), type, unitstatus);
                            _uow.CashUnits.Add(cashUnit);
                        }
                    
                

                }
                else
                {
                    var cashunits = atm.CashUnits;
                    foreach (var cu in command.Cashunit)
                    {
                        if (string.IsNullOrWhiteSpace(cu.UnitId)) continue;

                        var existing = cashunits.FirstOrDefault(x =>
                            string.Equals(x.Name, cu.UnitId, StringComparison.OrdinalIgnoreCase));

                        if (existing != null)
                        {
                            existing.TotalCount = cu.Init.ToString();
                            existing.CurrentCount = cu.Count.ToString();
                            existing.Denomination = cu.Denomination;
                        }
                        else
                        {
                            // کاست جدیدی که قبلاً ندیده‌ایم — به‌جای نادیده گرفتن، اضافه کن
                            var st = cu.Count == 0 ? CashUnitStatus.Empty
                                   : cu.Count < 500 ? CashUnitStatus.Low
                                   : CashUnitStatus.Full;

                            var type = (cu.UnitId == "LCU00" || cu.UnitId == "12345")
                                     ? CashUnitType.Reject : CashUnitType.Bill;

                            _uow.CashUnits.Add(CashUnit.Create(
                                atm.Id, cu.UnitId, cu.currency, Guid.NewGuid().ToString(),
                                cu.Denomination, cu.Init.ToString(), cu.Count.ToString(), type, st));

                         
                        }
                    }
                }

            }

            // ── محافظ سمت سرور برای وضعیت دوربین ────────────────────
            // اگر ایجنت خطای دوربین را تشخیص نداده باشد، اینجا اصلاح می‌شود.
            if (command.CameraStatus is not null)
            {
                ushort camDev = command.CameraStatus.Device;

                // 0=ONLINE  6=BUSY → سالم. هر چیز دیگری خطاست.
                bool cameraFaulty = camDev != 0 && camDev != 6;

                // وضعیت تک‌تک دوربین‌ها: 2 = CAMINOP
                if (!cameraFaulty && command.CameraStatus.Detailes is not null)
                {
                    cameraFaulty = command.CameraStatus.Detailes
                        .Any(d => d.Camera == 2 || d.Media == 2);
                }

                if (cameraFaulty && command.Mode != DeviceMode.Error)
                {
                  

                    command.Mode = DeviceMode.Error;
                }
            }

            atm.SetStatus(command.Mode, command.IsInservice);
            // ثبت بازه‌ی وضعیت برای گزارش آماده‌به‌کاری
            await TrackDeviceStateAsync(atm, command.Mode, DateTime.Now, ct: cancellationToken);
            mode = command.Mode;

            await _uow.SaveChangesAsync(cancellationToken);
            if(mode!=atmmode)
            await _hubContext.Clients.All.SendAsync("ReceiveDeviceStatusUpdate", atm.Ip, mode);

            return op.succedded();
        }
        private static HealthStatus MapHealthStatus(ushort status)
        {
            return status switch
            {
                0 => HealthStatus.Online, 
                1 => HealthStatus.Offline, 
                2 => HealthStatus.PowerOff,
                3 => HealthStatus.DeviceNotFound,
                4 => HealthStatus.HardwareError,
                5 => HealthStatus.UserError,
                6 => HealthStatus.Busy,
                7 => HealthStatus.FraudAttempt,
                8 => HealthStatus.PotentialFraud,
                _ => HealthStatus.Unknown, 
            };
        }
        /// <summary>
        /// آیا این وضعیت به معنای اختلال در کارکرد قطعه است؟
        ///
        /// DeviceNotFound عمداً خطا نیست — یعنی این قطعه اصلاً روی دستگاه
        /// نصب نشده. اگر خطا حسابش کنیم، هر ATMی که مثلاً دوربین ندارد
        /// برای همیشه «خراب» گزارش می‌شود.
        ///
        /// Busy هم موقتی است و در چرخه‌ی بعدی برطرف می‌شود.
        /// </summary>
        private static bool IsFaultState(HealthStatus s) => s switch
        {
            HealthStatus.Online => false,
            HealthStatus.Busy => false,
            HealthStatus.DeviceNotFound => false,
            HealthStatus.Unknown => false,   // نامشخص ≠ خراب

            HealthStatus.Offline => true,
            HealthStatus.PowerOff => true,
            HealthStatus.HardwareError => true,
            HealthStatus.UserError => true,
            HealthStatus.FraudAttempt => true,
            HealthStatus.PotentialFraud => true,

            _ => false,
        };
        private static string StatusFa(HealthStatus s) => s switch
        {
            HealthStatus.Online => "آنلاین",
            HealthStatus.Offline => "آفلاین",
            HealthStatus.PowerOff => "بدون برق",
            HealthStatus.DeviceNotFound => "دستگاه یافت نشد",
            HealthStatus.HardwareError => "خطای سخت‌افزاری",
            HealthStatus.UserError => "خطای کاربری",
            HealthStatus.Busy => "مشغول",
            HealthStatus.FraudAttempt => "تلاش برای تقلب",
            HealthStatus.PotentialFraud => "احتمال تقلب",
            _ => "نامشخص",
        };
        /// <summary>
        /// بازه‌ی خرابی را باز، به‌روز یا بسته می‌کند.
        ///
        /// فقط هنگام «تغییر» می‌نویسد، نه در هر چرخه‌ی متریک — بنابراین
        /// دستگاهی که یک ماه خراب بماند فقط یک ردیف دارد.
        /// </summary>
        /// <summary>
        /// بازه‌ی خرابی را باز، به‌روز یا بسته می‌کند.
        /// بازه‌های باز از ModuleContext می‌آیند، پس کوئری جدیدی نمی‌زند.
        /// </summary>
        private void TrackModuleFault(
            Device atm,
            Guid? moduleId,
            DeviceModuleType moduleType,
            HealthStatus newStatus,
            ushort rawStatus,
            DateTime now,
            ModuleContext mc)
        {
            bool isFaulty = IsFaultState(newStatus);

            mc.OpenFaults.TryGetValue(moduleType, out var openFault);

            if (isFaulty)
            {
                if (openFault is null)
                {
                    var fault = ModuleFaultLog.Open(
                        atm.Id, moduleId, moduleType,
                        newStatus, rawStatus, StatusFa(newStatus), now);

                    _uow.ModuleFaultLogs.Add(fault);
                    mc.OpenFaults[moduleType] = fault;
                }
                else
                {
                    openFault.Transition(newStatus, rawStatus, StatusFa(newStatus), now);
                }
            }
            else if (openFault is not null)
            {
                openFault.Resolve(now);
                mc.OpenFaults.Remove(moduleType);
            }
        }
        private const int MaxSnapshots = 10;
        /// <summary>
        /// بازه‌ی وضعیت دستگاه را باز، به‌روز یا جابه‌جا می‌کند.
        ///
        /// فقط هنگام **تغییر دسته** بازه‌ی جدید باز می‌شود. اگر Mode عوض
        /// شود ولی در همان دسته بماند (مثلاً InService → warning_Money)
        /// بازه پیوسته می‌ماند و فقط CurrentMode به‌روز می‌شود — چون از
        /// دید آماده‌به‌کاری هیچ اتفاقی نیفتاده.
        ///
        /// یعنی یک دستگاه سالم در طول یک ماه معمولاً **یک ردیف** دارد.
        /// </summary>
        private async Task TrackDeviceStateAsync(
            Device atm, DeviceMode mode, DateTime now, CancellationToken ct)
        {
            var newState = AvailabilityMapping.FromMode(mode);

            var open = await _uow.DeviceStateLogs
                .FirstOrDefaultAsync(x => x.DeviceId == atm.Id && x.EndedAt == null, ct);

            if (open is null)
            {
                _uow.DeviceStateLogs.Add(
                    DeviceStateLog.Open(atm.Id, newState, mode, now));
                return;
            }

            if (open.State == newState)
            {
                // همان دسته — فقط زنده بودن و Mode دقیق را ثبت کن
                open.Touch(mode, now);
                return;
            }

            // دسته عوض شد: بازه‌ی قبلی را ببند و بازه‌ی جدید باز کن.
            //
            // هر دو با **همان** لحظه‌ی now انجام می‌شوند تا بین دو بازه
            // شکاف نیفتد؛ وگرنه مجموع مدت‌ها با طول واقعی بازه نمی‌خواند و
            // درصدها کمی غلط درمی‌آیند.
            open.Close(now);
            _uow.DeviceStateLogs.Add(
                DeviceStateLog.Open(atm.Id, newState, mode, now));
        }

        private bool IsError(HealthStatus status)
        {
           
            return status is HealthStatus.UserError or HealthStatus.Offline or HealthStatus.HardwareError
                or HealthStatus.DeviceNotFound;
           
        }
        private async Task<bool> UpsertModuleStatusAsync(
    Device atm,
    DeviceModuleType moduleType,
    string displayName,
    ushort? deviceCode,             
    object statusPayload,
      ModuleContext mc,
    CancellationToken ct)
        {
            if (deviceCode == null) return false;

            var now = DateTime.Now;
            var newStatus = MapHealthStatus(deviceCode.Value);
            var json = JsonSerializer.Serialize(statusPayload);

            var module = atm.Modules.FirstOrDefault(x => x.Type == moduleType);
            if (module == null)
            {
                module = DeviceModule.Create(atm.Id, moduleType, displayName);
                atm.Modules.Add(module);
                _uow.DeviceModules.Add(module);
            }


            // از حافظه، نه دیتابیس
            mc.Statuses.TryGetValue(module.Id, out var currentStatus);

            if (currentStatus == null)
            {
                var snapshot = DeviceModuleStatusSnapshot.Create(module.Id, newStatus, json, 0, now);
                var status = DeviceModuleStatus.Create(module.Id, newStatus, json, 0);

                _uow.DeviceModuleStatusSnapshots.Add(snapshot);
                _uow.DeviceModuleStatuses.Add(status);

                // برای ماژول تازه‌ساخته‌شده، context را هم به‌روز کن
                mc.Statuses[module.Id] = status;
                mc.SnapshotCounts[module.Id] = 1;
            }
            else
            {
                if (currentStatus.Status != newStatus)
                {
                    mc.SnapshotCounts.TryGetValue(module.Id, out int count);

                    if (count >= MaxSnapshots &&
                        mc.OldestSnapshots.TryGetValue(module.Id, out var oldest))
                    {
                        // حلقه‌ی سقف‌دار: قدیمی‌ترین را بازنویسی کن
                        oldest.Status = newStatus;
                        oldest.StateJson = json;
                        oldest.Severity = 0;
                        oldest.ModifiedDate = now;
                        oldest.CapturedAt = now;
                    }
                    else
                    {
                        var snapshot = DeviceModuleStatusSnapshot.Create(module.Id, newStatus, json, 0, now);
                        _uow.DeviceModuleStatusSnapshots.Add(snapshot);
                        mc.SnapshotCounts[module.Id] = count + 1;
                    }
                }

                currentStatus.Update(newStatus, json, 0);
            }

            TrackModuleFault(atm, module.Id, moduleType, newStatus, deviceCode.Value, now, mc);

            return IsError(newStatus);
        }

        public async Task<OperationResult> UpdateAgentStatus(string Ip, CancellationToken cancellationToken = default)
        {
            var OP= new OperationResult();
            Device? atm = await _uow.Devices.Where(x=>x.Ip==Ip).FirstOrDefaultAsync(cancellationToken);
            if (atm != null) {
                atm.SetAgentOffLine();
                await _uow.SaveChangesAsync(cancellationToken);
            }
            return OP.succedded();
        }
    }
}
