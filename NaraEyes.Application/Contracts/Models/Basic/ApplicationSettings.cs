using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public class ApplicationSettings
    {
        public DatabaseConnection ConnectionStrings { get; set; } = null!;
    }
    public class DatabaseConnection
    {
        public string ApplicationDbContext { get; set; } = null!;
    }
}
