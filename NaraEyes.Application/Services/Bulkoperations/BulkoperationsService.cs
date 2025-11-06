using ClosedXML.Excel;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Interfaces.Bulkoperations;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Bulkoperations;
using NaraEyes.Application.Contracts.Models.Modules.Cam;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using NaraEyes.Domain.Entities.Devices;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Bulkoperations
{
    public sealed class BulkoperationsService(IApplicationUnitOfWork uow,
        IApplicationUserManager userManamager
        , Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment,
        AuthenticationStateProvider auth
,
        ICommandDispatchState dispatchState) : IBulkoperationsService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManamager = userManamager;
        private readonly ICommandDispatchState _dispatchState= dispatchState;
        private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment environment = _environment;

        public async Task<OperationResult> BulkFileUpload(IBrowserFile file, List<GroupedDeviceViewModel> SelectedDevice, string baseuri, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var CreatedUrl = "";
            try
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


                CreatedUrl = await Uploader.Upload(file, environment);

                var newOutBoxMwssage = OutBoxDeviceMessage.CreateForCampaign("255.255.255.0", userId, CommandType.UploadGroupFile, file.Name);
                _uow.OutBoxDeviceMessages.Add(newOutBoxMwssage);

                var campain = Campaign.createCampaign(type, newOutBoxMwssage.Id, EnumHelper.GetEnumDisplayName(type), userId);
                foreach (var device in SelectedDevice)
                {
                    var newTarget = CampaignTarget.CreateNewTarget(campain.Id, device.Ip, userId);

                    campain.NewTarget(newTarget);
                }
                var payload = new SendGroupInstructionModel
                {
                    MessageBoxId = newOutBoxMwssage.Id,
                    CampaignId = campain.Id,

                    Type = (int)type,
                    url = $"{baseuri}{CreatedUrl}"
                };
                var json = JsonSerializer.Serialize(payload);
                campain.ManifestJson = json;
                _uow.Campaigns.Add(campain);
                var targetUser = await _userManamager.GetUserBy(userId!.Value);
                if (targetUser != null)
                {
                    targetUser.SetLastCommand(CommandType.UploadGroupFile);
                }
                await _uow.SaveChangesAsync(cancellationToken);
                foreach (var device in campain.Targets)
                {
                    var key = ToolsDate.Key(device.DeviceIp);
                    _dispatchState.MarkCommandEnqueued(key);
                }
                return op.succedded();
            }
            catch (Exception ex)
            {
                Uploader.DeleteFile(CreatedUrl, environment);
                return op.Failed($"آپلود فایل با خطا مواجه شد {ex.Message}    inner {ex.InnerException?.Message}");

            }

        }

        public async Task<string> CreateExcelFileAsync(List<GroupedDeviceViewModel> SelectedDevice, OperationType type, CancellationToken cancellationToken = default)
        {
            var Ips = SelectedDevice.Select(x => x.Ip).ToList();
            switch (type)
            {
                case OperationType.None:
                    break;

                case OperationType.SystemResources:
                    {
                        return await CreateMetricExcel(Ips, cancellationToken);
                    }

                case OperationType.AgentVersion:
                    return await CreateAgentExcel(Ips, cancellationToken);


                case OperationType.CameraVersion:
                    return await CreateCameraExcel(Ips, cancellationToken);
                default:
                    return string.Empty;
            }

            return string.Empty;
        }



        public async Task<List<GroupedDeviceViewModel>> GetDevices(GroupedDeviceFilterViewModel filter, CancellationToken cancellationToken = default)
        {
            var DevicesQuery = _uow.Devices.AsNoTracking()
                .Include(x => x.Branch.Supervision)
                .Where(x => !x.Deleted);
            if (filter.BranchId.HasValue)
            {
                DevicesQuery = DevicesQuery.Where(x => x.BranchId == filter.BranchId.Value);
            }
            if (filter.SupervisionId.HasValue)
            {
                DevicesQuery = DevicesQuery.Where(x => x.Branch.SupervisionId == filter.SupervisionId.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = $"%{filter.SearchTerm.Trim()}%";
                DevicesQuery = DevicesQuery.Where(x =>
                    EF.Functions.Like(x.Ip, term) ||
                    EF.Functions.Like(x.Model, term) ||
                    EF.Functions.Like(x.SerialNo, term) ||
                    EF.Functions.Like(x.Branch.Name, term));
            }
            var list = await DevicesQuery.Select(x => new GroupedDeviceViewModel
            {
                SerialNo = x.SerialNo,
                Branch = x.Branch != null ? x.Branch.Name : null,
                supervisionId = x.Branch != null ? (Guid?)x.Branch.SupervisionId : null,
                BranchId = x.BranchId,
                Id = x.Id,
                Ip = x.Ip,
                Model = x.Model,
            }).ToListAsync();

            return list;

        }

        public async Task<OperationResult> RestartSelectedDevices(List<GroupedDeviceViewModel> SelectedDevice, CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            var CreatedUrl = "";
            try
            {


                var type = OperationType.Reset;
                var userId = _userManamager.UserId!.Value;


                var newOutBoxMwssage = OutBoxDeviceMessage.CreateForCampaign("255.255.255.0", userId, CommandType.ResetGroup);
                _uow.OutBoxDeviceMessages.Add(newOutBoxMwssage);

                var campain = Campaign.createCampaign(type, newOutBoxMwssage.Id, EnumHelper.GetEnumDisplayName(type), userId);
                foreach (var device in SelectedDevice)
                {
                    var newTarget = CampaignTarget.CreateNewTarget(campain.Id, device.Ip, userId);

                    campain.NewTarget(newTarget);
                }
                _uow.Campaigns.Add(campain);
                var payload = new SendGroupInstructionModel
                {
                    MessageBoxId = newOutBoxMwssage.Id,
                    CampaignId = campain.Id,
                    Type = (int)type
                };
                var json = JsonSerializer.Serialize(payload);
                campain.ManifestJson = json;
                var targetUser = await _userManamager.GetUserBy(userId);
                if (targetUser != null)
                {
                    targetUser.SetLastCommand(CommandType.ResetGroup);
                }
                await _uow.SaveChangesAsync(cancellationToken);
                foreach (var device in campain.Targets)
                {
                    var key = ToolsDate.Key(device.DeviceIp);
                    _dispatchState.MarkCommandEnqueued(key);
                }
                return op.succedded();
            }
            catch (Exception)
            {
                return op.Failed("عملیات با خطا مواجه شد");
            }
        }
        private async Task<string> CreateMetricExcel(List<string> Ips, CancellationToken cancellationToken)
        {
            var metrics = await _uow.Devices.AsNoTracking()
                  .Include(x => x.CurrentMetrics).Where(x => Ips.Contains(x.Ip))
                  .Select(x => new
                  {
                      DeviceIp = x.Ip,
                      Name = x.Model,
                      Ram = x.CurrentMetrics != null ? x.CurrentMetrics.TotalRamGb : 0,
                      RamUsage = x.CurrentMetrics != null ? x.CurrentMetrics.RamUsage : 0,
                      Cpu = x.CurrentMetrics != null ? x.CurrentMetrics.CpuModel : "نا مشخص",
                      cpuUsage = x.CurrentMetrics != null ? x.CurrentMetrics.CpuUsage : 0,
                      DiskUsage = x.CurrentMetrics != null ? x.CurrentMetrics.DiskUsage : 0,
                      Modified = x.CurrentMetrics.ModifiedDate.HasValue ? x.CurrentMetrics.ModifiedDate.Value.ToFarsiFull() : x.CurrentMetrics.ModifiedDate.ToFarsi()
                  }).ToListAsync(cancellationToken);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش منابع دستگاه‌ها");
            int r = 1;
            ws.Cell(r, 1).Value = "آی‌پی";
            ws.Cell(r, 2).Value = "نام دستگاه";
            ws.Cell(r, 3).Value = "RAM (GB)";
            ws.Cell(r, 4).Value = "مدل CPU";
            ws.Cell(r, 5).Value = "مصرف RAM";
            ws.Cell(r, 6).Value = "مصرف CPU";
            ws.Cell(r, 7).Value = "مصرف Disk";
            ws.Cell(r, 8).Value = "آخرین بروزرسانی";
            var header = ws.Range(r, 1, r, 7);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            r++;
            foreach (var m in metrics)
            {
                ws.Cell(r, 1).Value = m.DeviceIp;
                ws.Cell(r, 2).Value = m.Name ?? "";
                ws.Cell(r, 3).Value = m.Ram;

                var ramUsage = m.RamUsage / 100.0;

                ws.Cell(r, 5).Value = ramUsage; // به درصد تبدیل می‌کنیم
                ws.Cell(r, 5).Style.NumberFormat.Format = "0%";
                ws.Cell(r, 5).Style.Fill.BackgroundColor = ramUsage < 0.4 ? XLColor.LightGreen :
                                                ramUsage < 0.7 ? XLColor.LightYellow : XLColor.LightCoral;


                ws.Cell(r, 4).Value = m.Cpu ?? "نا مشخص";

                var cpuUsage = m.cpuUsage / 100.0;
                ws.Cell(r, 6).Value = cpuUsage;
                ws.Cell(r, 6).Style.NumberFormat.Format = "0%";
                ws.Cell(r, 6).Style.Fill.BackgroundColor = cpuUsage < 0.4 ? XLColor.LightGreen :
                                           cpuUsage < 0.7 ? XLColor.LightYellow : XLColor.LightCoral;

                var diskUsage = m.DiskUsage / 100.0;
                ws.Cell(r, 7).Value = diskUsage;
                ws.Cell(r, 7).Style.NumberFormat.Format = "0%";
                ws.Cell(r, 7).Style.Fill.BackgroundColor = diskUsage < 0.4 ? XLColor.LightGreen :
                                                 diskUsage < 0.7 ? XLColor.LightYellow : XLColor.LightCoral;
                ws.Cell(r, 8).Value = m.Modified ?? "نا مشخص";

                r++;
            }

            // 5) زیباسازی و UX
            var used = ws.RangeUsed();
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            used.Style.Font.FontName = "Tahoma"; // فونت رایج برای فارسی
            ws.SheetView.FreezeRows(1);          // فریز هدر
            used.SetAutoFilter();                 // فیلتر روی هدرها
            ws.Columns().AdjustToContents();      // عرض مناسب ستون‌ها

            // 6) تبدیل به Base64 (بدون ذخیره‌ی فیزیکی)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }
        public async Task<string> CreateCameraExcel(List<string> Ips, CancellationToken cancellationToken)
        {
            var metrics = await _uow.Devices.AsNoTracking()
                   .Include(x => x.Modules)
                   .Where(x => Ips.Contains(x.Ip))
                   .Select(x => new
                   {
                       DeviceIp = x.Ip,
                       Name = x.Model,
                       CameraId = x.Modules != null ? x.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Camera) : null,
                   }).ToListAsync(cancellationToken);

            var camerasmoduleId = metrics.Where(x => x.CameraId != null).Select(x => x.CameraId.Id).ToList();

            var cameras = await _uow.DeviceModuleStatuses.AsNoTracking()
                .Where(x => camerasmoduleId.Contains(x.DeviceModuleId))
                .ToListAsync(cancellationToken);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش منابع دستگاه‌ها");
            int r = 1;
            ws.Cell(r, 1).Value = "آی‌پی";
            ws.Cell(r, 2).Value = "نام دستگاه";
            ws.Cell(r, 3).Value = "وضعیت ماژول";
            ws.Cell(r, 4).Value = "وضعیت دوربین Room";
            ws.Cell(r, 5).Value = "وضعیت دوربین Person";
            ws.Cell(r, 6).Value = "وضعیت دوربین ExitSlot";
            ws.Cell(r, 7).Value = "تعداد عکس های ذخیره شده ROOM";
            ws.Cell(r, 8).Value = "تعداد عکس های ذخیره شده Person";
            ws.Cell(r, 9).Value = "تعداد عکس های ذخیره شده EXITSLOT";
            ws.Cell(r, 10).Value = "آخرین بروز رسانی";
            var header = ws.Range(r, 1, r, 10);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            r++;
            foreach (var m in cameras)
            {
                var dto = JsonSerializer.Deserialize<CameraStatusDto>(m.StateJson);
                var metric = metrics.FirstOrDefault(x => x.CameraId?.Id == m.DeviceModuleId);  // چک کردن CameraId برای null

                if (dto == null || metric == null) { continue; }  // اگر CameraStatusDto یا metric null باشد، ادامه بده

                var Room = dto.Detailes?.FirstOrDefault(x => x.Lable.ToLower().Trim() == "room");
                var Person = dto.Detailes?.FirstOrDefault(x => x.Lable.ToLower().Trim() == "person");
                var exitslot = dto.Detailes?.FirstOrDefault(x => x.Lable.ToLower().Trim() == "exitslot");

                ws.Cell(r, 1).Value = metric.DeviceIp;
                ws.Cell(r, 2).Value = metric.Name ?? "";
                ws.Cell(r, 3).Value = CDMHelper.MapDeviceStatusToPersian(dto.Device);

                ws.Cell(r, 4).Value = CameraHelper.MapMediaState(Room.Media);
                ws.Cell(r, 5).Value = CameraHelper.MapMediaState(Person.Media);
                ws.Cell(r, 6).Value = CameraHelper.MapMediaState(exitslot.Media);

                ws.Cell(r, 7).Value = CameraHelper.MapPicturesCount(Room.Pictures);
                ws.Cell(r, 8).Value = CameraHelper.MapPicturesCount(Person.Pictures);
                ws.Cell(r, 9).Value = CameraHelper.MapPicturesCount(exitslot.Pictures);
                ws.Cell(r, 10).Value = m.ModifiedDate.HasValue ? m.ModifiedDate.Value.ToFarsiFull() : m.CreateDate.ToFarsiFull();

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
        public async Task<string> CreateAgentExcel(List<string> Ips, CancellationToken cancellationToken)
        {
            var metrics = await _uow.Devices.AsNoTracking()

                   .Where(x => Ips.Contains(x.Ip))
                   .Select(x => new
                   {
                       DeviceIp = x.Ip,
                       Name = x.Model,
                       agentVersion = x.AgentVersion,
                       modifyDate = x.ModifiedDate.HasValue ? x.ModifiedDate.Value.ToFarsiFull() : x.ModifiedDate.ToFarsi(),
                   }).ToListAsync(cancellationToken);



            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش منابع دستگاه‌ها");
            int r = 1;
            ws.Cell(r, 1).Value = "آی‌پی";
            ws.Cell(r, 2).Value = "نام دستگاه";
            ws.Cell(r, 3).Value = "ورژن ایجنت";

            ws.Cell(r, 4).Value = "آخرین تاریخ راه اندازی";
            var header = ws.Range(r, 1, r, 4);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;

            r++;
            foreach (var m in metrics)
            {




                ws.Cell(r, 1).Value = m.DeviceIp;
                ws.Cell(r, 2).Value = m.Name ?? "";
                ws.Cell(r, 3).Value = m.agentVersion;

                ws.Cell(r, 4).Value = m.modifyDate;


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

    }
}
