using ClosedXML.Excel;
using Dapper;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Dapper;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.QueueAbstraction;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Interfaces.Devices;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.DapperModels;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Models.Metrics;
using NaraEyes.Application.Contracts.Models.Modules.Cam;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly AuthenticationStateProvider auth;
        private readonly IApplicationUserManager _userManamager;
        private readonly ICommandDispatchState _dispatchState;
        private static readonly CultureInfo _gregorian = CreateGregorian();
        private readonly IDbConnectionFactory _connectionFactory;
        // → یک چرخه‌ی کامل معطلی. پس هیچ تایم‌اوتی نباید زیر ۳۵ ثانیه باشد.
        private static readonly TimeSpan PollCycle = TimeSpan.FromSeconds(35);

        private static readonly TimeSpan AckTimeout = PollCycle + TimeSpan.FromSeconds(15);  // ۵۰
        private static readonly TimeSpan ScreenshotTimeout = PollCycle + TimeSpan.FromSeconds(25);  // ۶۰
        private static readonly TimeSpan StatusTimeout = PollCycle + TimeSpan.FromSeconds(20);  // ۵۵
        private static readonly TimeSpan JournalTimeout = PollCycle + TimeSpan.FromSeconds(40);

        public DeviceService(IApplicationUnitOfWork uow, ICommandAwaiter await, IOutboxService outbox, IAckAwaiter ack, AuthenticationStateProvider _auth,
                  IApplicationUserManager userManamager, ICommandDispatchState dispatchState, IDbConnectionFactory connectionFactory)
        {
            _uow = uow;
            _await = await;
            _outbox = outbox;
            _ack = ack;
            auth = _auth;
            _userManamager = userManamager;
            this._dispatchState = dispatchState;
            _connectionFactory = connectionFactory;
        }
        private async Task<Guid?> GetUserId()
        {
            var userId = _userManamager.UserId;
            var state = await auth.GetAuthenticationStateAsync();
            var user = state.User;
            var idValue = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(idValue, out var gid))
                userId = gid;
            else
                userId = null;
            var type = OperationType.FileSend;
            return userId;
        }
        public async Task DeactivateAsync(Guid deviceId,string reson, CancellationToken ct)
        {
            var userid = await GetUserId();
            if (userid is null)
                throw new InvalidOperationException("Current user id is null.");
            var device = await _uow.Devices.FirstOrDefaultAsync(d => d.Id == deviceId, ct);
            if (device == null)
                throw new InvalidOperationException($"Device with Id {deviceId} not found");


            device.Deactivate();
            var DeviceArchive= await _uow.ArchivedDevice.FirstOrDefaultAsync(x=>x.DeviceId == deviceId, ct);
            if(DeviceArchive == null)
            {
              var archived=  ArchivedDevice.CreateArchive(deviceId, userid.Value, reson);
                _uow.ArchivedDevice.Add(archived);
            }
            else
            {
                DeviceArchive.ArchivedAgain(userid.Value,reson);
            }
                await _uow.SaveChangesAsync(ct);
        }
        public async Task RestoreAsync(Guid deviceId, CancellationToken ct)
        {
         
            var userId = await GetUserId();
            if (userId is null)
                throw new InvalidOperationException("Current user id is null.");

           
            var device = await _uow.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId, ct);

            if (device is null)
                throw new InvalidOperationException($"Device with Id {deviceId} not found.");

      
            var archived = await _uow.ArchivedDevice
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId && !x.Deleted, ct);

            if (archived is null)
                throw new InvalidOperationException($"Active archive record for device {deviceId} not found.");


            device.Activate(); 


            archived.Restore(userId.Value);

            await _uow.SaveChangesAsync(ct);
        }
        public async Task<IReadOnlyList<ArcivedDeviceViewModel>> GatArchivedDevices(CancellationToken cancellationToken = default)
        {
            var query= await _uow.ArchivedDevice.AsNoTracking()
                .Include(c=>c.CreatedByUser)
                .Include(c=>c.Device)
                .Where(x=>!x.Deleted)
                .Select(x=>new ArcivedDeviceViewModel
                {
                    Id=x.DeviceId,
                    DeletedAt=x.ModifiedDate!=null?x.ModifiedDate.Value.ToFarsiFull():x.CreateDate.ToFarsiFull(),
                    DeletedBy = x.CreatedByUser != null
                      ? (x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName)
                         : "-",
                    DeleteReson =x.ArchiveReason,
                    Address = x.Device != null ? x.Device.Address : "-",
                    Code = x.Device != null ? x.Device.Code :null,
                    SerialNo = x.Device != null ? x.Device.SerialNo : "-",
                    Ip = x.Device != null ? x.Device.Ip : "-",
                    Model = x.Device != null ? x.Device.Ip : "-",
                })
                .ToListAsync(cancellationToken);
            return query;
        }

        public async Task<IReadOnlyList<GetDevicesViewModel>> GetDevicesAsync(CancellationToken cancellationToken)
        {
            var allDevices = await _uow.Devices.AsNoTracking()
                .Where(x=>!x.Deleted)
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
            var userid = await GetUserId();
            entity.ModifiedById = userid;
            var branchId = model.BranchId == Guid.Empty ? null : model.BranchId;
            entity.ApplyUpdate(model.Code, model.Ip, model.Model, model.InstallationDate,
                model.Address, model.SerialNo, model.Tel, model.MobileNo, branchId,
                model.Description, model.Latitude, model.Longitude, model.IsActive, null, null);
            entity.Operator = contact;
            if(contact!=null)
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
        public async Task<OperationResult> CreateDevice(CreateDeviceViewModel model, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var deviceCount = await _uow.Devices.AsNoTracking().CountAsync(cancellationToken);
            if (deviceCount >= 15)
            {
                return op.Failed("امکان اضافه کردن دستگاه بیشتر از 15 عدد وجود ندارد");
            }
            Device? entity = Device.RegisterNewDev(model.Code, model.Ip, model.Model, model.SerialNo, model.Address, model.Longitude,
                model.Latitude,model.Description,model.BranchId.Value,model.MobileNo);

            var userid =await GetUserId();
            entity.CreatedByUserId = userid;

            try
            {
                _uow.Devices.Add(entity);
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
            var deviceCount =await _uow.Devices.AsNoTracking().CountAsync(ct);
            if (deviceCount >= 15)
            {
                return Guid.Empty ;
            }
            var existing = await _uow.Devices.FirstOrDefaultAsync(d => d.Ip == context.ip, ct);
            if (existing != null)
            {

                existing.ReRegister(context.TerminalCode,context.ip, context.model, context.agentVersion);
                await _uow.SaveChangesAsync(ct);
                return existing.Id;
            }


            var device = Device.RegisterNew(context.TerminalCode, context.ip, context.model, context.serialNo, context.agentVersion, context.mode);
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
            try
            {

            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 200);

            // 1) کوئری پایه
            var baseQuery = _uow.Devices
                .AsNoTracking()
                .Include(d => d.Branch)
                .Include(d => d.CashUnits)
                .Where(x=>!x.Deleted)
                .AsQueryable();

            // فیلترها...
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                baseQuery = baseQuery.Where(d =>
                       EF.Functions.Like(d.Ip, $"%{s}%")
                    || (d.SerialNo != null && EF.Functions.Like(d.SerialNo, $"%{s}%"))
                    || (d.Model != null && EF.Functions.Like(d.Model, $"%{s}%"))
                    || (d.Branch != null && EF.Functions.Like(d.Branch.Name, $"%{s}%"))
                    || (d.Code != null && EF.Functions.Like(d.Code.ToString()!, $"%{s}%"))
                );
            }

            if (filter.Status.HasValue)
                baseQuery = baseQuery.Where(d => d.Mode == filter.Status.Value);

            if (filter.Branch.HasValue)
                baseQuery = baseQuery.Where(d => d.BranchId == filter.Branch.Value);

            // 2) یه کوئری برای total
            var totalQuery = baseQuery;

            // 3) مرتب‌سازی
            var desc = filter.SortDirection == SortDirectionDto.Descending;
            baseQuery = (filter.SortLabel?.ToLowerInvariant()) switch
            {
                "status" => desc ? baseQuery.OrderByDescending(d => d.Mode) : baseQuery.OrderBy(d => d.Mode),
                "name" => desc ? baseQuery.OrderByDescending(d => d.Code).ThenByDescending(d => d.SerialNo).ThenByDescending(d => d.Ip)
                               : baseQuery.OrderBy(d => d.Code).ThenBy(d => d.SerialNo).ThenBy(d => d.Ip),
                "ip" => desc ? baseQuery.OrderByDescending(d => d.Ip) : baseQuery.OrderBy(d => d.Ip),
                "branch" => desc ? baseQuery.OrderByDescending(d => d.Branch!.Name) : baseQuery.OrderBy(d => d.Branch!.Name),
                "seen" => desc ? baseQuery.OrderByDescending(d => d.LastHeartbeat)
                               : baseQuery.OrderBy(d => d.LastHeartbeat),
                "serial" => desc ? baseQuery.OrderByDescending(d => d.SerialNo) : baseQuery.OrderBy(d => d.SerialNo),
                _ => baseQuery.OrderBy(d => d.Code)
            };

            // 4) اول total رو بگیر
            var total = await totalQuery.CountAsync(cancellationToken);

            // 5) بعد صفحه رو بگیر
            var entities = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

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
            catch (Exception ex)
            {

                throw;
            }

        }

        public async Task<int> CheckHeartBeat( CancellationToken cancellationToken = default)
        {

            var deadlineUtc = DateTime.Now.AddMinutes(-10);

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
                    IsInservice=d.InService,
                    DisplayName = d.Code.ToString()??0.ToString(),
                    DeviceAgent=d.AgentStatus,
                    Ip = d.Ip,
                    SerialNo = d.SerialNo,
                    Model = d.Model ?? string.Empty,
                    Branch = d.Branch?.Name,                    // اگر navigation لود شد
                    Status = d.Mode,                            // DeviceMode → همان Status در VM
                    LastSeen = d.LastHeartbeat ?? DateTime.Now,
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
                CommandType = Domain.Enumerations.CommandType.Screenshot,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);
            _dispatchState.MarkCommandEnqueued(deviceIp);
            var bytes = await _await.WaitForBytesAsync(cmd.Id, ScreenshotTimeout, ct);
            return bytes;
        }
        public async Task<bool> RequestResetCdmAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = Domain.Enumerations.CommandType.ResetCdm,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);

            _dispatchState.MarkCommandEnqueued(deviceIp);
            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, AckTimeout, ct);
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
                CommandType = Domain.Enumerations.CommandType.Metrics,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) 
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);

                _dispatchState.MarkCommandEnqueued(deviceIp);
                var ack = await _ack.WaitForAckAsync(id, AckTimeout, ct);
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
            var devices = await _uow.Devices.AsNoTracking().Where(x=>!x.Deleted).ToListAsync(ct);
            var branchcount=await _uow.Branches.AsNoTracking().CountAsync(ct);
            var supervision = await _uow.SupervisionStates.AsNoTracking().CountAsync(ct);
            if (user != null) {
                name =user.LastName+ " "+user.FirstName;    
                    }
            return new HomeChartsViewModel
            {
                InServiceCount = devices.Where(x => x.Mode==DeviceMode.InService).Count(),
                errorCount = devices.Where(x => x.Mode == DeviceMode.Error).Count(),
                warningCount = devices.Where(x => x.Mode is DeviceMode.warning or DeviceMode.warning_Money or DeviceMode.warning_paper).Count(),
                offlineCount = devices.Where(x => x.Mode == DeviceMode.Offline).Count(),
                OnlineCount= devices.Where(x => x.Mode == DeviceMode.Online).Count(),
                OutOfService= devices.Where(x => x.Mode != DeviceMode.InService).Count() ,
                TotalDevice =devices.Count(),
                BranchCount= branchcount,
                Supervisions= supervision,
                Name=name,
                TotalUsers=usersCount,
                inserviceErrors=devices.Where(x=>x.InService&&x.Mode==DeviceMode.Error).Count(),
                inserviceWarning = devices.Where(x => x.InService && (x.Mode == DeviceMode.warning||x.Mode==DeviceMode.warning_paper||x.Mode==DeviceMode.warning_Money)).Count(),
                OutofserviceErrors= devices.Where(x => !x.InService && x.Mode == DeviceMode.Error).Count(),
                OutOfserviceWarning = devices.Where(x => !x.InService && (x.Mode == DeviceMode.warning || x.Mode == DeviceMode.warning_paper || x.Mode == DeviceMode.warning_Money)).Count(),
            };
                
        }

        public async Task<bool> RequestResetPtrAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = Domain.Enumerations.CommandType.testprinter,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);

            _dispatchState.MarkCommandEnqueued(deviceIp);
            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, AckTimeout, ct);
            return ack.Accepted;
        }
        public async Task<bool> RequestResetIdcAsync(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); // ⬅️ خودت بساز
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         // ⬅️ مهم
                DeviceIp = deviceIp,
                CommandType = Domain.Enumerations.CommandType.resetIdc,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) // (اختیاری)
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);

            _dispatchState.MarkCommandEnqueued(deviceIp);
            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, AckTimeout, ct);
            return ack.Accepted;
        }
        public async Task<bool> GetForcesStatus(string deviceIp, CancellationToken ct = default)
        {
            var id = Guid.NewGuid(); 
            var cmd = new OutBoxDeviceMessage
            {
                Id = id,                         
                DeviceIp = deviceIp,
                CommandType = Domain.Enumerations.CommandType.GetForcesStatus,
                Payload = JsonSerializer.Serialize(new { CommandId = id }) 
            };
            await _outbox.EnqueueCommandAsync(cmd, ct);
            _dispatchState.MarkCommandEnqueued(deviceIp);


            var ack = await _ack.WaitForAckAsync(id, StatusTimeout, ct);
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
                CommandType = Domain.Enumerations.CommandType.EJournal,
                Payload = JsonSerializer.Serialize(new { CommandId = id })
            };

            await _outbox.EnqueueCommandAsync(cmd, ct);
            _dispatchState.MarkCommandEnqueued(deviceIp);

            var bytes = await _await.WaitForBytesAsync(cmd.Id, JournalTimeout, ct);
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
            var type = isrestart ? Domain.Enumerations.CommandType.Reset : Domain.Enumerations.CommandType.Shutdown;

            var cmd = new OutBoxDeviceMessage
            {
                Id = id,
                DeviceIp = deviceIp,
                CommandType = type,
                Payload = JsonSerializer.Serialize(new { CommandId = id, Reason = "ManualAction", DelaySeconds = 5 })
            };

            await _outbox.EnqueueCommandAsync(cmd, cancellationToken);
            _dispatchState.MarkCommandEnqueued(deviceIp);
            // منتظر ACK از ایجنت
            var ack = await _ack.WaitForAckAsync(id, AckTimeout, cancellationToken);
            return ack.Accepted;

        }

        public async Task<DeviceMetricsViewModel> GetMetricsAsync(string deviceIp, CancellationToken cancellationToken = default)
        {
            try
            {

         
            const string sql = @"
SELECT TOP (1)
    d.Ip,
    d.Model              AS DeviceModel,
    d.SerialNo           AS DeviceSerial,
    d.Code,
d.Address,
m.OsInfo,
m.AgentTime,
    d.InstallationDate,
    b.ShortName          AS BranchShortName,
    o.PhoneNumber        AS OperatorMobile,
    o.Name               AS OperatorName,
    m.DiskUsage,
    m.CpuUsage,
    m.RamUsage,
    m.AgentVersion,
    m.CpuModel,
    m.ModifiedDate       AS MetricsModifiedDate,
    m.TotalRamGb
FROM Devices d
    LEFT JOIN MetricSnapshots m ON d.CurrentMetricsId = m.Id
    LEFT JOIN ContactInfos o     ON d.OperatorId       = o.Id
    LEFT JOIN Branches b        ON d.BranchId         = b.Id
WHERE d.Ip = @Ip";

            using IDbConnection conn = _connectionFactory.GetOpenConnection();

            var row = await conn.QueryFirstOrDefaultAsync<DeviceMetricsRow>(
                sql,
                new { Ip = deviceIp });

            if (row is null)
                return new DeviceMetricsViewModel();
                string? HumanDrift = "ساعت با سرور هماهنگ است";
                if (row.MetricsModifiedDate != null)
                    { 
                   var drift = row.AgentTime - row.MetricsModifiedDate;
                     HumanDrift = FormatTimeDrift(drift.Value); }



            var vm = new DeviceMetricsViewModel
            {
                Ip = row.Ip,
                code=row.Code??0,
                DeviceModel = row.DeviceModel,
                DeviceSerial = row.DeviceSerial,
                Address=row.Address,
                WinModel=row.OsInfo,
                Drift=HumanDrift,

                DiskUsage = new ChartMetrics
                {
                    usage = row.DiskUsage ?? 0
                },

                CpuModel = row.CpuModel,
                DisplayName = row.Code?.ToString(),

                AgentVersion = row.AgentVersion,
                InstallationDate = row.InstallationDate.ToFarsi(),     
                Branch = row.BranchShortName,

                CpuUsage = new ChartMetrics
                {
                    usage = row.CpuUsage ?? 0
                },

                LastHeartBeat = row.MetricsModifiedDate?.ToFarsiFull(), 

                OperatorMobile = row.OperatorMobile,
                OperatorName = row.OperatorName,

                RamUsage = new ChartMetrics
                {
                    usage = row.RamUsage ?? 0
                },

                TotalRam = (row.TotalRamGb?.ToString() ?? "0")
            };

                return vm;
            }
            catch (Exception ex)
            {

                throw;
            }


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
                CommandType = Domain.Enumerations.CommandType.UploadFile,  // نوع فرمان برای آپلود فایل
                Payload = JsonSerializer.Serialize(new { CommandId = id, FileData = base64FileContent,Extension= extension,Name= name })  // اطلاعات فایل
            };

            await _outbox.EnqueueCommandAsync(cmd, ct);
            _dispatchState.MarkCommandEnqueued(deviceIp);

            var ack = await _ack.WaitForAckAsync(id, AckTimeout, ct);
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

        public async Task<string> ExportExcelAsync(DeviceFilterViewModel filter, CancellationToken cts= default)
        {
            if(filter.Branch==null&&filter.Search==null&&filter.Status==null)
            {
                filter.Page = 1;
                filter.PageSize = 3000;
            }
            var Devices = await GetAllDevicesAsync(filter);
            List<DeviceViewModel>? items = Devices.Items;
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش  دستگاه‌ها");
            int r = 1;
            ws.Cell(r, 1).Value = "آی‌پی";
            ws.Cell(r, 2).Value = "وضعیت";
            ws.Cell(r, 3).Value = "نام دستگاه";
            ws.Cell(r, 4).Value = "شعبه";
            ws.Cell(r, 5).Value = "سریال";
            ws.Cell(r, 6).Value = "موجودی";
            ws.Cell(r, 7).Value = "بروزرسانی";

            var header = ws.Range(r, 1, r, 6);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            r++;
            foreach (var i in items)
            {
                
                ws.Cell(r, 1).Value = i.Ip;
                ws.Cell(r, 2).Value = EnumHelper.GetEnumDisplayName(i.Status) ?? "";
                ws.Cell(r, 2).Style.Fill.BackgroundColor = GetColor(i.Status);

                ws.Cell(r, 3).Value = i.DisplayName;
                ws.Cell(r, 4).Value = i.Branch;
                ws.Cell(r, 5).Value = i.SerialNo;
                ws.Cell(r, 6).Value = i.CashInventory;

                ws.Cell(r, 7).Value = i.UpdatedAt.ToFarsiFull();



                r++;
            }

            // زیباسازی و UX
            var used = ws.RangeUsed();
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            used.Style.Font.FontName = "Tahoma"; // فونت رایج برای فارسی
            ws.SheetView.FreezeRows(1);          // فریز هدر
            used.SetAutoFilter();                 // فیلتر روی هدرها
            ws.Columns().AdjustToContents();      // عرض مناسب ستون‌ها

            // تبدیل به Base64 (بدون ذخیره‌ی فیزیکی)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);


        }

        private static XLColor GetColor(DeviceMode status)
        {
            switch (status)
            {
                case DeviceMode.InService:
                    return XLColor.LightGreen;
                   
                case DeviceMode.Supervisor:
                    return XLColor.LightBlue;
                    
                case DeviceMode.Offline:
                    return XLColor.LightGray;
                case DeviceMode.Error:
                    return XLColor.LightPink;
                case DeviceMode.warning:
                    return XLColor.LightYellow;
                default: return XLColor.White;
            }
        }

        public async Task<bool> AddDeviceWithExcel(IBrowserFile file, CancellationToken cancellationToken = default)
        {
            if (file is null)
                return false;
            var deviceCount = await _uow.Devices.AsNoTracking().CountAsync(cancellationToken);
            if (deviceCount >= 15)
            {
                return  false;
            }
            await using var stream = file.OpenReadStream(5 * 1024 * 1024, cancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
                return false;

            var range = ws.RangeUsed();
            if (range == null)
                return false;

            // همه کدهای موجود

            var branches = await _uow.Branches.AsNoTracking()
                .Select(x =>new { x.Id, x.Code }).ToListAsync(cancellationToken);

            var userId = await GetUserId();
            if (userId == null || userId == Guid.Empty)
                return false;

            var newEntities = new List<Device>();
            var codesInFile = new HashSet<int>();

            foreach (var row in range.RowsUsed().Skip(1))
            {

      
                var codeCell = row.Cell(1);
                string codeText;

                if (codeCell.DataType == XLDataType.Number)
                {

                    codeText = ((int)codeCell.GetDouble()).ToString();
                }
                else
                {
                    codeText = codeCell.GetString()?.Trim();
                }




                var Ip = row.Cell(2).GetString()?.Trim();
                var BranchCode = row.Cell(3).GetString()?.Trim();
                if(BranchCode==null)
                    return false;
                var branchId=branches.Where(x=>x.Code==int.Parse(BranchCode)).Select(x=>x.Id).FirstOrDefault();
                var Model = row.Cell(4).GetString()?.Trim();
                var Serial = row.Cell(5).GetString()?.Trim();

                var Address = row.Cell(6).GetString()?.Trim();
                var Latitude = row.Cell(7).GetString()?.Trim();
                var Longtitude = row.Cell(8).GetString()?.Trim();
                var Description = row.Cell(9).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(codeText))
                    continue;

                if (!int.TryParse(codeText, out var code))
                    continue;


                if (!codesInFile.Add(code))
                    continue;

            var lon=string.IsNullOrEmpty(Longtitude)?0:decimal.Parse(Longtitude);
                var lat = string.IsNullOrEmpty(Latitude) ? 0 : decimal.Parse(Latitude);

                Device? entity = Device.RegisterNewDev(int.Parse(codeText), Ip, Model, Serial, Address, lon,
                    lat, Description, branchId, null);

                var userid = await GetUserId();
                entity.CreatedByUserId = userid;


                newEntities.Add(entity);
            }

            if (newEntities.Count == 0)
                return false;

            _uow.Devices.AddRange(newEntities);
            await _uow.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<string?> GetSampleFileForDownload(CancellationToken cancellationToken = default)
        {
            using var wb = new XLWorkbook();


            var ws = wb.Worksheets.Add("دستگاه ها");
            ws.Cell(1, 1).Value = "کد";
            ws.Cell(1, 2).Value = "آیپی";
            ws.Cell(1, 3).Value = "کد شعبه";
            ws.Cell(1, 4).Value = "مدل";
            ws.Cell(1, 5).Value = "شماره سریال";
            ws.Cell(1, 6).Value = "آدرس ";
            ws.Cell(1, 7).Value = "عرض جغرافیایی";
            ws.Cell(1, 8).Value = "طول جغرافیایی";
            ws.Cell(1, 9).Value = "توضیحات";


            var headerRange = ws.Range("A1:H1");
            headerRange.Style.Font.Bold = true;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return await Task.FromResult(base64); 


          
        }

        public async Task<string?> GetDeviceReport(CancellationToken cancellationToken = default)
        {
            var list = await _uow.Devices.Include(c=>c.Branch)
          .AsNoTracking()
          .Select(x => new
          {
              x.Code,
              x.Ip,
              x.Branch.Name,
              x.Model,
              x.SerialNo,
              x.Address,
              x.Latitude,
              x.Longitude,
              x.Description
          })
          .OrderBy(x => x.Code)
          .ToListAsync(cancellationToken);

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Supervisions");

            ws.Cell(1, 1).Value = "کد دستگاه";
            ws.Cell(1, 2).Value = "آیپی";
            ws.Cell(1, 3).Value = " شعبه";
            ws.Cell(1, 4).Value = "مدل";
            ws.Cell(1, 5).Value = "شماره سریال ";
            ws.Cell(1, 6).Value = "آدرس";
            ws.Cell(1, 7).Value = "عرض چغرافیایی";
            ws.Cell(1, 8).Value = "طول چغرافیایی";
            ws.Cell(1, 9).Value = "توضیحات";

            ws.Range("A1:I1").Style.Font.Bold = true;

            var row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.Code;
                ws.Cell(row, 2).Value = item.Ip;
                ws.Cell(row, 3).Value = item.Name;
                ws.Cell(row, 4).Value = item.Model;
                ws.Cell(row, 5).Value = item.SerialNo;
                ws.Cell(row, 6).Value = item.Address;
                ws.Cell(row, 7).Value = item.Latitude;
                ws.Cell(row, 8).Value = item.Longitude;
                ws.Cell(row, 9).Value = item.Description;
                row++;
            }


            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }
        private static string FormatTimeDrift(TimeSpan drift)
        {
            var abs = drift.Duration(); // مقدار قدرمطلق اختلاف

            // اگر اختلاف خیلی کم بود، بگو تقریبا هماهنگ است
            if (abs < TimeSpan.FromSeconds(5))
                return "ساعت دستگاه با سرور هماهنگ است (اختلاف کمتر از ۵ ثانیه).";

            string direction = drift > TimeSpan.Zero ? "جلوتر" : "عقب‌تر";

            var parts = new List<string>();

            if (abs.Days > 0)
                parts.Add($"{abs.Days} روز");

            if (abs.Hours > 0)
                parts.Add($"{abs.Hours} ساعت");

            if (abs.Minutes > 0)
                parts.Add($"{abs.Minutes} دقیقه");

            // فقط اگر روز/ساعت/دقیقه نداشتیم، ثانیه را نشان بده
            if (abs.Days == 0 && abs.Hours == 0 && abs.Minutes == 0 && abs.Seconds > 0)
                parts.Add($"{abs.Seconds} ثانیه");

            var spanText = string.Join(" و ", parts);

            return $"ساعت دستگاه حدود {spanText} از سرور {direction} است.";
        }


  
    }


}
