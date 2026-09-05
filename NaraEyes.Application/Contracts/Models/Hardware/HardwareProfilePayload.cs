namespace NaraEyes.Application.Contracts.Models.Hardware
{
    /// <summary>
    /// ⚠️ باید با NaraEyesAgent.Core.Models.Hardware.HardwareProfileDto
    /// دقیقاً یکی بماند. نام پراپرتی‌ها روی سیم منتقل می‌شوند؛ تغییر یک
    /// نام یعنی آن فیلد اینجا null می‌شود — بدون خطا، فقط بی‌سروصدا.
    /// </summary>
    public sealed class HardwareProfilePayload
    {
        public int RamTotalMb { get; set; }
        public List<RamModulePayload> RamModules { get; set; } = new();

        public string? CpuName { get; set; }
        public int CpuCores { get; set; }
        public int CpuLogicalProcessors { get; set; }
        public int CpuMaxClockMhz { get; set; }
        public string? CpuId { get; set; }

        public string? DiskModel { get; set; }
        public long DiskSizeBytes { get; set; }
        public string? DiskSerial { get; set; }
        public string? DiskInterface { get; set; }

        public string? BoardManufacturer { get; set; }
        public string? BoardProduct { get; set; }
        public string? BoardSerial { get; set; }
        public string? BiosVersion { get; set; }

        public string? ComputerName { get; set; }
        public string? OsVersion { get; set; }

        public DateTime CollectedAt { get; set; }
        public bool IsComplete { get; set; }
    }

    public sealed class RamModulePayload
    {
        public int CapacityMb { get; set; }
        public string? Manufacturer { get; set; }
        public string? PartNumber { get; set; }
        public string? SerialNumber { get; set; }
        public string? DeviceLocator { get; set; }
        public int SpeedMhz { get; set; }
    }
}