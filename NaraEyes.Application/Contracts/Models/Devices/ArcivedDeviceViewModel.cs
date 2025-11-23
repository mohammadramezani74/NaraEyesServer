namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class ArcivedDeviceViewModel
    {
        public Guid Id { get; set; }
        public int? Code { get; set; }
        public string Ip { get; set; }
        public string? Model { get; set; }
        public string? Address { get; set; }
        public string? SerialNo { get; set; }
        public string DeleteReson { get; set; } = null!;
        public string DeletedBy { get; set; } = null!;
        public string DeletedAt { get; set; } = null!;
    }
}
