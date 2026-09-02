using NaraEyes.Domain.Enumerations;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public sealed class CreateDeviceViewModel
    {
        public int? Code { get; set; }
        public string Ip { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public Guid? BranchId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? MobileNo { get; set; }
        public string? Model { get; set; }
        public string? SerialNo { get; set; }
        public DeviceVendor Vendor { get; set; } = DeviceVendor.Unknown;


    }
}
