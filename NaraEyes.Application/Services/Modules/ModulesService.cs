using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Modules;
using NaraEyes.Application.Contracts.Models.Modules;
using NaraEyes.Application.Contracts.Models.Modules.Cam;
using NaraEyes.Application.Contracts.Models.Modules.CDM;
using NaraEyes.Application.Contracts.Models.Modules.Idc;
using NaraEyes.Application.Contracts.Models.Modules.Pin;
using NaraEyes.Application.Contracts.Models.Modules.Ptr;
using NaraEyes.Application.Contracts.Models.Modules.SIU;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Modules
{
    public class ModulesService(IApplicationUnitOfWork uow) : IModuleServices
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<List<XfsModule>> GetModulesStatus(string DeviceIp, CancellationToken cancellationToken = default)
        {
            try
            {

     
            var list = ModuleCreationHelper.CreateStableModules();
            List<Domain.Entities.Devices.DeviceModuleStatus>? modulStatuses = new();

            var Atm = await _uow.Devices.AsNoTracking().Include(x => x.Modules)
                                        .FirstOrDefaultAsync(x => x.Ip == DeviceIp, cancellationToken);
            if (Atm == null)
            {
                return list;
            }

            var modulesId = Atm.Modules.Select(x => x.Id).ToList();

            if (modulesId.Any())
            {
                modulStatuses = await _uow.DeviceModuleStatuses.AsNoTracking()
                                  .Where(x => modulesId.Contains(x.DeviceModuleId)).ToListAsync(cancellationToken);
            }

            if (modulesId != null && modulStatuses != null)
            {
                var cdmModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Cdm);
                var IdcModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Idc);
                var ptrModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Ptr);
                var CameraModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Camera);
                var SensorsModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Siu);
                var PinModule = Atm.Modules.FirstOrDefault(x => x.Type == DeviceModuleType.Pin);

                if (cdmModule != null)

                {
                    Domain.Entities.Devices.DeviceModuleStatus? cdmStatus = modulStatuses
                                                .FirstOrDefault(x => x.DeviceModuleId == cdmModule.Id);


                    if (cdmStatus != null)
                    {
                        var cdm = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.Dispenser);
                        if (cdm != null)
                        {
                            cdm.HealthStatus = cdmStatus.Status;
                            cdm.ModuleId = cdmModule.Id;
                            cdm.Status = CDMHelper.MapDeviceStatusToPersian((ushort)cdmStatus.Status);
                        }
                    }
                }
                if (IdcModule != null)
                {
                    var idcStatus = modulStatuses
                                                     .FirstOrDefault(x => x.DeviceModuleId == IdcModule.Id);
                    var idc = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.IDC);
                    if (idc != null)
                    {
                        idc.HealthStatus = idcStatus.Status;
                        idc.ModuleId = IdcModule.Id;
                        idc.Status = CDMHelper.MapDeviceStatusToPersian((ushort)idcStatus.Status);
                    }
                   }
                    if (ptrModule != null)
                    {
                        var ptrStatus = modulStatuses
                                                           .FirstOrDefault(x => x.DeviceModuleId == ptrModule.Id);
                        var ptr = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.Ptr);
                        if (ptr != null)
                        {
                            ptr.HealthStatus = ptrStatus.Status;
                            ptr.ModuleId = ptrModule.Id;
                            ptr.Status = CDMHelper.MapDeviceStatusToPersian((ushort)ptrStatus.Status);
                        }
                    }

                    if (CameraModule != null)
                    {
                        var cameraStatus = modulStatuses
                                                              .FirstOrDefault(x => x.DeviceModuleId == CameraModule.Id);
                        var camera = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.Cam); // Adjust module name as needed
                        if (camera != null)
                        {
                            camera.HealthStatus = cameraStatus.Status;
                            camera.ModuleId = CameraModule.Id;
                            camera.Status = CDMHelper.MapDeviceStatusToPersian((ushort)cameraStatus.Status);
                        }
                    }

                    if (SensorsModule != null)
                    {
                        var sensorsStatus = modulStatuses
                                                               .FirstOrDefault(x => x.DeviceModuleId == SensorsModule.Id);
                        var sensors = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.Sensors);
                        if (sensors != null)
                        {
                            sensors.HealthStatus = sensorsStatus.Status;
                            sensors.ModuleId = SensorsModule.Id;
                            sensors.Status = CDMHelper.MapDeviceStatusToPersian((ushort)sensorsStatus.Status);
                        }
                    }

                    if (PinModule != null)
                    {
                        var pinStatus = modulStatuses
                                                           .FirstOrDefault(x => x.DeviceModuleId == PinModule.Id);
                        var pin = list.FirstOrDefault(x => x.Name == ModuleCreationHelper.Pin);
                        if (pin != null)
                        {
                            pin.HealthStatus = pinStatus.Status;
                            pin.ModuleId = PinModule.Id;
                            pin.Status = CDMHelper.MapDeviceStatusToPersian((ushort)pinStatus.Status);
                        }
                    }


                

            }
            return list;
            }
            catch (Exception ex)
            {

                return new List<XfsModule>();
            }
        }
        public async Task<List<Cassette>> GetCassetInfo(string deviceIp, CancellationToken cancellationToken = default)
        {
            try
            {

            var result = new List<Cassette>();

            var atm = await _uow.Devices
                .AsNoTracking()
                .Include(x => x.CashUnits)
                .FirstOrDefaultAsync(x => x.Ip == deviceIp, cancellationToken);

            if (atm?.CashUnits == null || atm.CashUnits.Count == 0)
                return result;


            var ordered = atm.CashUnits
                .OrderBy(cu => GetUnitIndex(cu.Name))
                .ThenBy(cu => cu.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var cash in ordered)
            {
                // اگر TotalCount رشته‌ای است، با TryParse امن بخوان
                int total = 0;
                _ = int.TryParse(cash.TotalCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out total);

                result.Add(new Cassette(
                    cash.Name,
                    cash.Denomination,
                    total,

                    cash.CurrentCountValue,
                    type:cash.Name=="LCU00"?"ریجکت":"پرداختی",
                   cash.Currency,
                   ((int.Parse(cash.CurrentCount)* cash.Denomination)/10).ToMoney() +" تومان"
                ));
            }

            return result;

            static int GetUnitIndex(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return int.MaxValue;
              
                var m = Regex.Match(name, @"\d+");
                if (m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return n;
                return int.MaxValue; 
            }

            }
            catch (Exception ex)
            {

                return new List<Cassette>();
            }
        }
        public async Task<CdmModuleViewModel> GetCdmInfoAndChart(Guid moduleId, CancellationToken ct = default)
        {
            var result = new CdmModuleViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceModuleId == moduleId, ct);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<CdmStatusDto>(moduleStatus.StateJson);
            if (dto == null)
                return result;

            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.Device = CDMHelper.MapDeviceStatusToPersian(dto.Device);
            result.Dispenser = CDMHelper.MapDispenserStatus(dto.Dispenser);
            result.SafeDoor = CDMHelper.MapSafeDoorStatus(dto.SafeDoor);
            result.IntermediateStacker = CDMHelper.MapStackerStatus(dto.IntermediateStacker);


            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == moduleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(ct);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }

        public async Task<IdcModuleViewModel> GetIdcInfo(Guid ModuleId, CancellationToken cancellationToken = default)
        {
            var result = new IdcModuleViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceModuleId == ModuleId, cancellationToken);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<IdcStatusDto>(moduleStatus.StateJson);
            if (dto == null)
                return result;

            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.Device = CDMHelper.MapDeviceStatusToPersian(dto.Device);
            result.Media = IdcHelper.GetMediaText(dto.Media);
            result.ChipPower = IdcHelper.GetChipPowerText(dto.ChipPower);
            result.RetainBin = IdcHelper.GetRetainBinText(dto.RetainBin);


            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == ModuleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(cancellationToken);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }

        public async Task<PinStatusViewModel> GetPinInfo(Guid ModuleId, CancellationToken cancellationToken = default)
        {
            var result = new PinStatusViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceModuleId == ModuleId, cancellationToken);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<PinStatusDto>(moduleStatus.StateJson);
            if (dto == null)
                return result;

            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.Device = CDMHelper.MapDeviceStatusToPersian(dto.Device);



            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == ModuleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(cancellationToken);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }
        public async Task<PtrModuleViewModel>GetPtrInfo(Guid moduleId, CancellationToken cancellationToken = default)
        {
            var result = new PtrModuleViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceModuleId == moduleId, cancellationToken);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<PtrStatusDto>(moduleStatus.StateJson);
            if (dto == null)
                return result;

            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.Device = CDMHelper.MapDeviceStatusToPersian(dto.Device);
            result.Ink=PtrHelper.GetInkStatus(dto.Ink);
            result.Toner=PtrHelper.GetTonerStatus(dto.Toner);
            result.Media=PtrHelper.GetMediaStatus(dto.Media);
            result.Paper = EnumHelper.GetEnumDisplayName(dto.Paper);



            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == moduleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(cancellationToken);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }

        public async Task<CameraStatusViewModel> GetCameraInfo(Guid ModuleId, CancellationToken cancellationToken = default)
        {
            var result = new CameraStatusViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
              .FirstOrDefaultAsync(x => x.DeviceModuleId == ModuleId, cancellationToken);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<CameraStatusDto>(moduleStatus.StateJson);
            if (dto == null)
                return result;
        

            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.Device = CDMHelper.MapDeviceStatusToPersian(dto.Device);
result.AntiFraudModule=CameraHelper.MapAntiFraudModuleStatus(dto.AntiFraudModule);
            foreach (var cam in dto.Detailes)
            {
                if (cam.Lable.ToLower().Trim() == "room")
                    result.ROOM = new CameraType
                    {
                        lable=CameraHelper.MapCameraIndexToFa(cam.Lable.Trim()),
                        Media=CameraHelper.MapMediaState(cam.Media),
                        Picture=CameraHelper.MapPicturesCount(cam.Pictures),
                        Camera=CameraHelper.MapCameraState(cam.Camera),
                    };
                if (cam.Lable.ToLower().Trim() == "person")
                    result.PERSON = new CameraType
                    {
                        lable = CameraHelper.MapCameraIndexToFa(cam.Lable.Trim()),
                        Media = CameraHelper.MapMediaState(cam.Media),
                        Picture = CameraHelper.MapPicturesCount(cam.Pictures),
                        Camera = CameraHelper.MapCameraState(cam.Camera),
                    };
                if (cam.Lable.ToLower().Trim() == "exitslot")
                    result.EXITSLOT = new CameraType
                    {
                        lable = CameraHelper.MapCameraIndexToFa(cam.Lable.Trim()),
                        Media = CameraHelper.MapMediaState(cam.Media),
                        Picture = CameraHelper.MapPicturesCount(cam.Pictures),
                        Camera = CameraHelper.MapCameraState(cam.Camera),
                    };
            }


            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == ModuleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(cancellationToken);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }
        public async Task<SiuModuleViewModel> GetSiuInfo(Guid ModuleId, CancellationToken cancellationToken = default)
        {
            var result = new SiuModuleViewModel();

            var moduleStatus = await _uow.DeviceModuleStatuses.AsNoTracking()
              .FirstOrDefaultAsync(x => x.DeviceModuleId == ModuleId, cancellationToken);


            if (moduleStatus == null)
                return result;


            if (string.IsNullOrWhiteSpace(moduleStatus.StateJson))
                return result;

            var dto = JsonSerializer.Deserialize<SiuStatusModel>(moduleStatus.StateJson);
            if (dto == null)
                return result;


            result.LastUpdate = (moduleStatus.ModifiedDate ?? moduleStatus.CreateDate).ToFarsiFull();
            result.DeviceStatusFa = CDMHelper.MapDeviceStatusToPersian(dto.Device);
            var siuFarsi = dto.ToPersian();
            result = siuFarsi;


            var chartData = await _uow.DeviceModuleStatusSnapshots.AsNoTracking()
                .Where(x => x.DeviceModuleId == ModuleId)
                .OrderBy(x => x.CreateDate)
                .Take(10)
                .ToListAsync(cancellationToken);


            result.Times = chartData.Select(x => x.CreateDate).ToArray();
            result.Lables = chartData.Select(x => x.CreateDate.ToString("HH:mm")).ToArray();

            result.status = chartData.Select(x => (int)x.Status).ToArray();



            return result;
        }

        public async Task<bool> IsDeviceInservice(string ip, CancellationToken cancellationToken = default)
        {
            return await _uow.Devices.AnyAsync(x => x.Ip == ip && x.Mode == DeviceMode.InService);
        }
    }


}
