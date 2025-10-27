using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.BulkOperation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Base
{
    public class OutBoxDeviceMessage:BaseEntity
    {
        public string DeviceIp { get; set; } = string.Empty;
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedAt { get; set; }
        public Enumerations.CommandType CommandType { get; set; }
        public string Payload { get; set; } = string.Empty;
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public Campaign?  Campaign { get; set; }
        public static OutBoxDeviceMessage CreateForCampaign(string Ip,Guid? userId)
       => new OutBoxDeviceMessage
       {
         Id=Guid.NewGuid(),
         Deleted=false,
         DeviceIp=Ip,
         CreateDate=DateTime.Now,
         CreatedByUserId= userId,
         CommandType=Enumerations.CommandType.UploadFile,
       };
    }
}
