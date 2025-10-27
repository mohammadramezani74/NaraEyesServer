using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class DeviceViewModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string? SerialNo { get; set; }
        public string Model { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public DeviceMode Status { get; set; }

        public DateTime LastSeen { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string? LastCommand { get; set; }

        public int CashInventory { get; set; }
    }
}
