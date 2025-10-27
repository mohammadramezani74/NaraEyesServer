using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class GetDevicesViewModel
    {
        public Guid Id { get; set; }
        public int? Code { get;  set; }
        public string Ip { get;  set; }
        public string? Model { get;  set; }
        public DateTime InstallationDate { get;  set; }
        public string? Address { get;  set; }
        public string? SerialNo { get;  set; }
        public string? Tel { get;  set; }
        public string? MobileNo { get;  set; }
        public Branch? Branch { get;  set; }
        public Guid? BranchId { get;  set; }
        public DeviceMode Mode { get;  set; }
        public string? Description { get;  set; }
        public decimal? Latitude { get;  set; }
        public decimal? Longitude { get;  set; }
        public bool IsActive { get;  set; }
        public string? OperatorName { get;  set; }
        public string? OperatorAddress { get; set; }
        public string? OperatorEmail { get; set; }
        public string? OperatorTel { get; set; }
        public string? OperatorphoneNumber{ get; set; }
        public Guid? OperatorId { get;  set; }
        public string? AgentVersion { get;  set; }
    }
}
