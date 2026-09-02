using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public sealed class UpdateDeviceViewModel
    {
        public Guid Id { get; set; }
        public int? Code { get; set; }
        public string Ip { get; set; }
        public string? Description { get; set; }
        public DateTime? InstallationDate { get; set; }
        public string? Address { get; set; }
        public string? AgentVersion { get; set; }
        public Guid? BranchId { get; set; }
        public bool IsActive { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? MobileNo { get; set; }
        public string? Model { get; set; }
        public DeviceVendor Vendor { get; set; } = DeviceVendor.Unknown;
        public string? SerialNo { get; set; }
        public string? Tel { get; set; }
        public newcontact newContact { get; set; }
    }
    public record newcontact(


              string? Name,
  string Tel ,
     string PhoneNumber,
     string Address ,
     string Email);

}
