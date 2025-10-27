namespace NaraEyes.Application.Contracts.Models.Modules.Pin
{
    public class PinStatusViewModel
    {
        public string Device { get; set; }
        public string? LastUpdate { get; set; }
        public DateTime[]? Times { get; set; }
        public string[]? Lables { get; set; }
        public int[]? status { get; set; }
    }
}
