using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Enumerations
{
    public enum CommandType
    {
        Reset = 1,          
        Screenshot = 2,       
        CashUnitStatus = 3,  
        DeviceStatus = 4,   
        UpdateConfig = 5,    
        Shutdown = 6,    
        SendLogs = 7,
        EJournal=8,
        ResetCdm = 9,
        resetIdc = 10,
        testprinter = 11,
        UploadFile=12,
        Metrics = 13,
        UploadGroupFile=14,
        ResetGroup=15,

    }
}
