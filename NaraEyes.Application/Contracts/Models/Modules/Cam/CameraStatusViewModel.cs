using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Modules.Cam
{
    public sealed class CameraStatusViewModel
    {
        public string Device { get; set; }
        public string AntiFraudModule { get; set; }
        public string LastUpdate { get; set; }
        public CameraType? ROOM { get; set; }
        public CameraType? PERSON { get; set; }
        public CameraType? EXITSLOT { get; set; }
        public DateTime[]? Times { get; set; }
        public string[]? Lables { get; set; }
        public int[]? status { get; set; }
    }
    public class CameraType
    {
        public string? lable { get; set; }
        public string? Camera { get; set; }
        public string? Media { get; set; }
        public string? Picture { get; set; }
    }
}
