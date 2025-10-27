using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public abstract class BaseCommand
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeviceIp { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
    }
}
