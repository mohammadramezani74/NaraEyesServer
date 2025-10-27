using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum DeviceMode
    {
        InService=1,
        Supervisor=2,
        warning=3,
        Error=4,
        Offline=5,
        Online=6

    }
}
