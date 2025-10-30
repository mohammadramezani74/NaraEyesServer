using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    public class FileUploadReport
    {
        public string? Ip { get; set; }
        public string? FileName { get; set; }
        public string? UploadDate { get; set; }
        public string? UploadedBy { get; set; }
        public bool SaveFile { get; set; }
        public string? SaveTime { get; set; }
    }
}
