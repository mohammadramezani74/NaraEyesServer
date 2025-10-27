using Microsoft.AspNetCore.SignalR;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Hubs
{
    public class DeviceHub : Hub
    {
        public async Task UpdateDeviceStatus(string ip, DeviceMode mode)
        {
         
            await Clients.All.SendAsync("ReceiveDeviceStatusUpdate", ip, mode);
        }
    }
}
