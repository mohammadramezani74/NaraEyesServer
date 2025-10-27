using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Devices
{
    public class DeviceService : IDeviceService
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IOutboxService _outbox;
        private readonly ICommandAwaiter _await;
        private readonly IAckAwaiter _ack;
        private static readonly CultureInfo _gregorian = CreateGregorian();

        public DeviceService(IApplicationUnitOfWork uow, ICommandAwaiter await, IOutboxService outbox, IAckAwaiter ack)
        {
            _uow = uow;
            _await = await;
            _outbox = outbox;
            _ack = ack;
        }

        public async Task DeactivateAsync(Guid deviceId, CancellationToken ct)
        {
            var device = await _uow.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, ct);
            if (device == null)
                throw new InvalidOperationException($"Device with Id {deviceId} not found");

            device.Deactivate();
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<GetDevicesViewModel>> GetDevicesAsync(CancellationToken cancellationToken)
        {
            var allDevices = await _uow.Devices.AsNoTracking()
                  .Select(x => new GetDevicesViewModel
                  {
                      Ip = x.Ip,
                      Code = x.Code,
                      Description = x.Description,
                      InstallationDate = x.InstallationDate,
                      Address = x.Address,
                      AgentVersion = x.AgentVersion,
                      Branch = x.Branch,
                      BranchId = x.BranchId,
                      Id = x.Id,
                      IsActive = x.IsActive,
                      Latitude = x.Latitude,
                      Longitude = x.Longitude,
                      MobileNo = x.MobileNo,
                      Mode = x.Mode,
                      Model = x.Model,
                      SerialNo = x.SerialNo,
                      OperatorAddress = x.Operator.Address,
                      OperatorName = x.Operator.Name,
                      OperatorEmail = x.Operator.Email,
                      OperatorphoneNumber = x.Operator.PhoneNumber,
                      OperatorTel = x.Operator.Tel,
                      OperatorId = x.Operator.Id,
                      Tel = x.Tel,

                  }).ToListAsync(cancellationToken);
            return allDevices;
        }
        public async Task<OperationResult> UpdateDevice_LegacyAsync(UpdateDeviceViewModel model, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            ContactInfo? contact = null;
            Device? entity = await _uow.Devices.Include(X => X.Branch)
                .Include(X => X.Operator).FirstOrDefaultAsync(d => d.Id == model.Id, cancellationToken);
            if (entity is null) return op.NotFound("دستگاه موردنظر یافت نشد");
            if (model.newContact != null)
            {

                contact = ContactInfo.Build(model.newContact.Name,
                    model.newContact.Tel, model.newContact.PhoneNumber,
                    model.newContact.Address, model.newContact.Email);
            }
            entity.ApplyUpdate(model.Code, model.Ip, model.Model, model.InstallationDate,
                model.Address, model.SerialNo, model.Tel, model.MobileNo, model.BranchId,
                model.Description, model.Latitude, model.Longitude, model.IsActive, null, null);
            entity.Operator = contact;
            entity.OperatorId = contact.Id;


            try
            {
                await _uow.SaveChangesAsync(cancellationToken);
                return op.succedded();
            }
            catch (DbUpdateException ex)
            {
                return op.Failed($"خطا در به‌روزرسانی دستگاه: {ex.GetBaseException().Message}");
            }
        }


        public async Task<Guid> RegisterAsync(RegisterDeviceCommand context, CancellationToken ct)
        {

            var existing = await _uow.Devices.FirstOrDefaultAsync(d => d.Ip == context.ip, ct);
            if (existing != null)
            {

                existing.ReRegister(context.ip, context.model, context.agentVersion);
                await _uow.SaveChangesAsync(ct);
                return existing.Id;
            }


            var device = Device.RegisterNew(context.code, context.ip, context.model, context.serialNo, context.agentVersion, context.mode);
            await _uow.Devices.AddAsync(device, ct);
            try
            {
                await _uow.SaveChangesAsync(ct);
            }
            catch (Exception)
            {

                throw;
            }


            return device.Id;
        }

        public async Task<Guid> ReRegisterAsync(string ip, string model, string? agentVersion, CancellationToken ct)
        {
            var device = await _uow.Devices.FirstOrDefaultAsync(d => d.Ip == ip, ct);
            if (device == null)
                throw new InvalidOperationException($"Device with IP {ip} not found");

            device.ReRegister(ip, model, agentVersion);
            await _uow.SaveChangesAsync(ct);

            return device.Id;
        }

        public async Task UpdateHeartbeatAsync(string ip, CancellationToken ct)
        {
            Device? device = await _uow.Devices.FirstOrDefaultAsync(d => d.Ip == ip, ct);
            if (device == null)
                throw new InvalidOperationException($"Device with IP {ip} not found");

            device.UpdateHeartbeat();
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<PageResultDto<DeviceViewModel>> GetAllDevicesAsync(DeviceFilterViewModel filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 200);

            
            var query = _uow.Devices
                .AsNoTracking()
                .Include(d => d.Branch)     // برای نام شعبه
                .Include(d => d.CashUnits)
                .OrderBy(x => x.Mode == DeviceMode.Error)
                .ThenBy(x => x.Mode == DeviceMode.warning)
                .AsQueryable();
            

            // ----- فیلترها -----
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();

                query = query.Where(d =>
                       EF.Functions.Like(d.Ip, $"%{s}%")
                    || (d.SerialNo != null && EF.Functions.Like(d.SerialNo, $"%{s}%"))
                    || (d.Model != null && EF.Functions.Like(d.Model, $"%{s}%"))
                    || (d.Branch != null && EF.Functions.Like(d.Branch.Name, $"%{s}%"))
                    || (d.Code != null && EF.Functions.Like(d.Code.ToString()!, $"%{s}%"))
                );
            }

            if (filter.Status.HasValue)
                query = query.Where(d => d.Mode == filter.Status.Value);

            if (filter.Branch.HasValue)
                query = query.Where(d => d.BranchId == filter.Branch.Value);

            var desc = filter.SortDirection == SortDirectionDto.Descending;
            query = (filter.SortLabel?.ToLowerInvariant()) switch
            {
                "status" => desc ? query.OrderByDescending(d => d.Mode) : query.OrderBy(d => d.Mode),
                "name" => desc ? query.OrderByDescending(d => d.Code).ThenByDescending(d => d.SerialNo).ThenByDescending(d => d.Ip)
                                  : query.OrderBy(d => d.Code).ThenBy(d => d.SerialNo).ThenBy(d => d.Ip),
                "ip" => desc ? query.OrderByDescending(d => d.Ip) : query.OrderBy(d => d.Ip),
                "branch" => desc ? query.OrderByDescending(d => d.Branch!.Name) : query.OrderBy(d => d.Branch!.Name),
                "seen" => desc ? query.OrderByDescending(d => d.LastHeartbeat)
                                  : query.OrderBy(d => d.LastHeartbeat),
                "serial" => desc ? query.OrderByDescending(d => d.SerialNo) : query.OrderBy(d => d.SerialNo),
                "cash" or "updated" or null or "" => query.OrderBy(d => d.Code),
                _ => query.OrderBy(d => d.Code)
            };


            var entities = await query.OrderByDescending(x => x.Mode == DeviceMode.Error)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);


            var total = await query.CountAsync(cancellationToken);

            // ----- مپ به ViewModel + محاسبات -----
            var list = entities.Select(MapToViewModel).ToList();

            // سورت ثانویه در حافظه برای ستون‌های محاسباتی
            if (!string.IsNullOrWhiteSpace(filter.SortLabel))
            {
                switch (filter.SortLabel.ToLowerInvariant())
                {
                    case "cash":
                        list = (desc ? list.OrderByDescending(x => x.CashInventory)
                                     : list.OrderBy(x => x.CashInventory)).ToList();
                        break;
                    case "updated":
                        list = (desc ? list.OrderByDescending(x => x.UpdatedAt)
                                     : list.OrderBy(x => x.UpdatedAt)).ToList();
                        break;
                }
            }

            return new PageResultDto<DeviceViewModel>
            {
                Items = list,
                Total = total
            };


        }

        public async Task<int> CheckHeartBeat( CancellationToken cancellationToken = default)
        {

            var deadlineUtc = DateTime.Now.AddHours(-1);

            var staleDevices = await _uow.Devices
                .Where(d => d.IsActive == true &&
                            (d.LastHeartbeat == null || d.LastHeartbeat < deadlineUtc))
                .ToListAsync(cancellationToken);

            foreach (var d in staleDevices)
            {
                d.SetOffline();
          
                d.ModifiedDate = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync(cancellationToken);
            return staleDevices.Count;

        }



        private DeviceViewModel MapToViewModel(NaraEyes.Domain.Entities.Devices.Device d)
        {
            // نام نمایش: بر اساس Code/Serial/IP
            var display = d.Code.HasValue
                ? $"ATM-{d.Code.Value}"
                : (!string.IsNullOrWhiteSpace(d.SerialNo) ? d.SerialNo! : d.Ip);
   
                return new DeviceViewModel
                {
                    Id = d.Id,
                    DisplayName = display,
                    Ip = d.Ip,
                    SerialNo = d.SerialNo,
                    Model = d.Model ?? string.Empty,
                    Branch = d.Branch?.Name,                    // اگر navigation لود شد
                    Status = d.Mode,                            // DeviceMode → همان Status در VM
                    LastSeen = d.LastHeartbeat ?? DateTime.MinValue,
                    UpdatedAt = GuessUpdatedAt(d),                 // توضیح زیر
                    LastCommand = "AllDeviceStatus",
                    CashInventory = SafeSumCash(d),
                    // محاسبه امن
                };
            }
        

        private static DateTime GuessUpdatedAt(NaraEyes.Domain.Entities.Devices.Device d)
        {

            if (d.LastHeartbeat.HasValue) return d.LastHeartbeat.Value;

            return d.ModifiedDate != null ? d.ModifiedDate.Value : d.CreateDate;
        }

        private static int SafeSumCash(NaraEyes.Domain.Entities.Devices.Device d)
        {

            var sum = 0;
            foreach (var cu in d.CashUnits)
            {
                var count = 0;
                _ = int.TryParse(cu.CurrentCount, out count);
                var denom = cu.Denomination; // فرض: int
                try
                {
                    checked { sum += denom * count; }
                }
                catch { /* جلوگیری از overflow؛ می‌تونی به long تغییر بدی */ }
            }
            return sum/10;
        }

        public async Task<byte[]> RequestScreenshotAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = CommandType.Screenshot,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);
            var bytes = await _await.WaitForBytesAsync(cmd.Id, TimeSpan.FromSeconds(60), ct);
            return bytes;
        }
        public async Task<bool> RequestResetCdmAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = CommandType.ResetCdm,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);


            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), ct);
            return ack.Accepted;
        }
        public async Task<DeviceMetricsViewModel?> RequestGetMetricsAsync(string deviceIp, CancellationToken ct = default)
        {
            try
            {

            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = CommandType.Metrics,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) 
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);


            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), ct);
            if (ack.Accepted)
            {
                var metrics = await _uow.Devices.AsNoTracking()
                    .Include(x => x.CurrentMetrics)
                    .Where(x => x.Ip == deviceIp)
                    .Select(x=>new DeviceMetricsViewModel
                    {
                        CpuUsage= new ChartMetrics
                        {
usage=x.CurrentMetrics.CpuUsage??0,

                        },
                        RamUsage = new ChartMetrics
                        {
                            usage = x.CurrentMetrics.RamUsage ?? 0,

                        },
                        DiskUsage = new ChartMetrics
                        {
                            usage = x.CurrentMetrics.DiskUsage ?? 0,

                        }
                    }).FirstOrDefaultAsync();
                    return metrics;
            }
            return null;

            }
            catch (Exception ex)
            {

               return null;
            }
        }
        public async Task<HomeChartsViewModel> GetVisualizeHome( string userName,CancellationToken ct=default)
        {
            var name = "خوش آمدید!";
            var user= _uow.Users.Where(x=>x.UserName.ToLower().Trim()==userName.ToLower().Trim()).FirstOrDefault();
            var usersCount= await _uow.Users.CountAsync();
            var devices = await _uow.Devices.AsNoTracking().ToListAsync(ct);
            var branchcount=await _uow.Branches.AsNoTracking().CountAsync(ct);
            var supervision = await _uow.SupervisionStates.AsNoTracking().CountAsync(ct);
            if (user != null) {
                name =user.LastName+ " "+user.FirstName;    
                    }
            return new HomeChartsViewModel
            {
                InServiceCount = devices.Where(x => x.Mode == DeviceMode.InService).Count(),
                errorCount = devices.Where(x => x.Mode == DeviceMode.Error).Count(),
                warningCount = devices.Where(x => x.Mode == DeviceMode.warning).Count(),
                offlineCount = devices.Where(x => x.Mode == DeviceMode.Offline).Count(),
                OnlineCount= devices.Where(x => x.Mode == DeviceMode.Online).Count(),
                TotalDevice =devices.Count(),
                BranchCount= branchcount,
                Supervisions= supervision,
                Name=name,
                TotalUsers=usersCount,

            };
                
        }

        public async Task<bool> RequestResetPtrAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = CommandType.testprinter,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);


            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), ct);
            return ack.Accepted;
        }
        public async Task<bool> RequestResetIdcAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = CommandType.resetIdc,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);


            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), ct);
            return ack.Accepted;
        }
        public async Task<byte[]?> RequestJournalAsync(string deviceIp, DateTime startLocal, DateTime endLocal, CancellationToken ct = default)
        {
            if (endLocal < startLocal) (startLocal, endLocal) = (endLocal, startLocal);

            var startDay = startLocal.Date;
            var endDay = endLocal.Date;

            var id = Guid.NewGuid();
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,
                DeviceIp = deviceIp,
                StartDate = startDay.ToString("yyyyMMdd", _gregorian),
                EndDate = endDay.ToString("yyyyMMdd", _gregorian),
                CommandType = CommandType.EJournal,
                Payload = JsonSerializer.Serialize(new { CommandId = id })
            };

            await _outbox.EnqueueCommandAsync(cmd, ct);


            var bytes = await _await.WaitForBytesAsync(cmd.Id, TimeSpan.FromSeconds(60), ct);
            return (bytes is { Length: > 0 }) ? bytes : null;
        }

        private static CultureInfo CreateGregorian()
        {
            var ci = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            ci.DateTimeFormat.Calendar = new GregorianCalendar();
            return ci;
        }
        public async Task<bool> RestartOrShutdownDevice(bool isrestart, string deviceIp, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid();
            var type = isrestart ? CommandType.Reset : CommandType.Shutdown;

            var cmd = new OutBoxDeviceMessage
            {
                Id = id,
                DeviceIp = deviceIp,
                CommandType = type,
                Payload = JsonSerializer.Serialize(new { CommandId = id, Reason = "ManualAction", DelaySeconds = 5 })
            };

            await _outbox.EnqueueCommandAsync(cmd, cancellationToken);

            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), cancellationToken);
            return ack.Accepted;

        }

        public async Task<DeviceMetricsViewModel> GetMetricsAsync(string deviceIp, CancellationToken cancellationToken = default)
        {
            var targetDevice = await _uow.Devices.
                Include(m => m.CurrentMetrics)
                .Include(x => x.Operator)
                .Include(x => x.Branch)
                .Where(x => x.Ip == deviceIp).AsNoTracking()
                .Select(x => new DeviceMetricsViewModel
                {
                    Ip = x.Ip,
                    DeviceModel = x.Model,
                    DeviceSerial = x.SerialNo,
                    DiskUsage = new ChartMetrics { usage = x.CurrentMetrics.DiskUsage ?? 0 },
                    CpuModel = x.CurrentMetrics.CpuModel,
                    DisplayName = x.Code.ToString(),
                    AgentVersion = x.CurrentMetrics.AgentVersion,
                    InstallationDate = x.InstallationDate.ToFarsi(),
                    Branch = x.Branch.ShortName,
                    CpuUsage = new ChartMetrics { usage = x.CurrentMetrics.CpuUsage ?? 0 },
                    LastHeartBeat = x.CurrentMetrics.ModifiedDate.Value.ToFarsiFull(),
                    OperatorMobile = x.Operator.PhoneNumber,
                    OperatorName = x.Operator.Name,
                    RamUsage = new ChartMetrics { usage = x.CurrentMetrics.RamUsage ?? 0 },
                    TotalRam = x.CurrentMetrics.TotalRamGb.ToString() ?? "0",

                }).FirstOrDefaultAsync(cancellationToken);
            if (targetDevice == null)
            {
                return new DeviceMetricsViewModel();
            }
            return targetDevice;


        }

        public async Task<bool> RequestUploadFileAsync(string deviceIp, IBrowserFile file, CancellationToken ct = default)
        {
            var id = Guid.NewGuid();  // یک شناسه جدید برای درخواست ایجاد کن
            var fileContent = await ConvertFileToBytesAsync(file); // فایل به byte[] تبدیل می‌شود
            var base64FileContent = Convert.ToBase64String(fileContent); // تبدیل به Base64 برای ارسال
            var extension = Path.GetExtension(file.Name);
            var name = Path.GetFileName(file.Name);
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,
                DeviceIp = deviceIp,
                CommandType = CommandType.UploadFile,  // نوع فرمان برای آپلود فایل
                Payload = JsonSerializer.Serialize(new { CommandId = id, FileData = base64FileContent,Extension= extension,Name= name })  // اطلاعات فایل
            };

            await _outbox.EnqueueCommandAsync(cmd, ct);

          
            var ack = await _ack.WaitForAckAsync(id, TimeSpan.FromSeconds(15), ct);
            return ack.Accepted;
        }
        public async Task<byte[]> ConvertFileToBytesAsync(IBrowserFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 1L * 1024 * 1024 * 1024).CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<List<BranchErrorAggDto>> GetTop10BranchesByErrorsAsync(CancellationToken ct = default)
        {
            var lastPerModule =
                from s in _uow.DeviceModuleStatuses.AsNoTracking()
                where s.Status != null && s.Deleted == false
                group s by s.DeviceModuleId into g
                select new
                {
                    DeviceModuleId = g.Key,
                    MaxCreateDate = g.Max(x => x.CreateDate)
                };

            var latestStatuses =
                from s in _uow.DeviceModuleStatuses.AsNoTracking()
                join lm in lastPerModule
                    on new { s.DeviceModuleId, s.CreateDate }
                    equals new { lm.DeviceModuleId, CreateDate = lm.MaxCreateDate }
                where s.Deleted == false
                select new
                {
                    s.DeviceModuleId,
                    s.Status,
                    s.Severity,
                    s.CreateDate
                };

            var q =
                from ls in latestStatuses
                where ls.Status > 0
                join dm in _uow.DeviceModules.AsNoTracking()
                    on ls.DeviceModuleId equals dm.Id
                where dm.Deleted == false
                join d in _uow.Devices.AsNoTracking()
                    on dm.DeviceId equals d.Id
                where d.Deleted == false
                join br in _uow.Branches.AsNoTracking()
                    on d.BranchId equals br.Id
                group new { dm, d } by new { d.BranchId, br.Name } into g
                select new BranchErrorAggDto
                {
                    BranchName = g.Key.Name,
                    ErrorModules = g.Count(),
                    AffectedDevices = g.Select(x => x.dm.DeviceId).Distinct().Count()
                };

            var result = await q
                .OrderByDescending(x => x.ErrorModules)
                .ThenByDescending(x => x.AffectedDevices)
                .Take(10)
                .ToListAsync(ct);
            return result;
        }
    }

}
