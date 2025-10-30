using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Reports
{
    public class UserActivityReport
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? LastLoginDate { get; set; }
        public string? LastCommand { get; set; }
        public string? LastCommandTime { get; set; }
    }
}
