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

            if (command.CdmStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
    atm,
    DeviceModuleType.Cdm,
    "CashDispenser",
    command.CdmStatus.Device,
    command.CdmStatus,
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
                                                              cancellationToken);
            
            }
            if (command.PinStatus != null)
            {
                haveError |= await UpsertModuleStatusAsync(
                                                              atm,
                                                              DeviceModuleType.Pin,
                                                              "Encryptor",
                                                              command.PinStatus.Device,
                                                              command.PinStatus,cancellationToken);
           
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
                            if (cu.UnitId == "LCU00"|| cu.UnitId == "1234")
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
                        if (cu.UnitId == "LCU00")
                        {
                            var unit0 = cashunits.Where(x => x.Name == "LCU00").FirstOrDefault();
                            if (unit0 != null)
                            {
                                unit0.TotalCount = cu.Init.ToString();
                                unit0.CurrentCount = cu.Count.ToString();
                                unit0.Denomination = cu.Denomination;

                            }

                        }
                        if (cu.UnitId == "LCU01")
                        {
                            var unit0 = cashunits.Where(x => x.Name == "LCU01").FirstOrDefault();
                            if (unit0 != null)
                            {
                                unit0.TotalCount = cu.Init.ToString();
                                unit0.CurrentCount = cu.Count.ToString();
                                unit0.Denomination = cu.Denomination;

                            }

                        }
                        if (cu.UnitId == "LCU02")
                        {
                            var unit0 = cashunits.Where(x => x.Name == "LCU02").FirstOrDefault();
                            if (unit0 != null)
                            {
                                unit0.TotalCount = cu.Init.ToString();
                                unit0.CurrentCount = cu.Count.ToString();
                                unit0.Denomination = cu.Denomination;

                            }

                        }
                        if (cu.UnitId == "LCU03")
                        {
                            var unit0 = cashunits.Where(x => x.Name == "LCU03").FirstOrDefault();
                            if (unit0 != null)
                            {
                                unit0.TotalCount = cu.Init.ToString();
                                unit0.CurrentCount = cu.Count.ToString();
                                unit0.Denomination = cu.Denomination;

                            }

                        }
                        if (cu.UnitId == "LCU04")
                        {
                            var unit0 = cashunits.Where(x => x.Name == "LCU04").FirstOrDefault();
                            if (unit0 != null)
                            {
                                unit0.TotalCount = cu.Init.ToString();
                                unit0.CurrentCount = cu.Count.ToString();
                                unit0.Denomination = cu.Denomination;

                            }

                        }
                    }
                }

            }

            atm.SetStatus(command.Mode,command.IsInservice);

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
        private const int MaxSnapshots = 10;

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


            var currentStatus = await _uow.DeviceModuleStatuses
                .FirstOrDefaultAsync(x => x.DeviceModuleId == module.Id, ct);

            if (currentStatus == null)
            {
                var snapshot = DeviceModuleStatusSnapshot.Create(module.Id, newStatus, json, 0, now);
                var status = DeviceModuleStatus.Create(module.Id, newStatus, json, 0);

                _uow.DeviceModuleStatusSnapshots.Add(snapshot);
                _uow.DeviceModuleStatuses.Add(status);
            }
            else
            {
              
                if (currentStatus.Status != newStatus)
                {
                
                    var count = await _uow.DeviceModuleStatusSnapshots
                        .Where(s => s.DeviceModuleId == module.Id)
                        .CountAsync(ct);

                    if (count >= MaxSnapshots)
                    {
                       
                        var oldest = await _uow.DeviceModuleStatusSnapshots
                            .Where(s => s.DeviceModuleId == module.Id)
                            .OrderBy(s => s.CreateDate) 
                            .FirstOrDefaultAsync(ct);

                     
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
                    }
                }

           
                currentStatus.Update(newStatus, json, 0);
            }

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
