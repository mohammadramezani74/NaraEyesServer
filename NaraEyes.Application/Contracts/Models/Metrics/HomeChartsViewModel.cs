using SixLabors.ImageSharp.Processing;

namespace NaraEyes.Application.Contracts.Models.Metrics
{
    public class HomeChartsViewModel
    {
        public int InServiceCount { get; set; }
        public int warningCount { get; set; }
        public int OutOfService { get; set; }
        public int errorCount { get; set; }
        public int OnlineCount { get; set; }
        public int offlineCount { get; set; }
        public int BranchCount { get; set; }
        public int TotalDevice { get; set; }
        public int Supervisions { get; set; }
        public string Name { get; set; }
        public int TotalUsers { get; set; }
        public int inserviceErrors { get; set; }
        public int inserviceWarning { get; set; }
        public int OutofserviceErrors { get; set; }
        public int OutOfserviceWarning { get; set; }


    }
}
