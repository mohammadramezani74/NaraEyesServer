using NaraEyes.Domain.Common;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.BulkOperation
{
    public class Campaign:BaseEntity
    {
        public OperationType OperationType { get; private set; }
        public string Title { get; private set; } = null!;
        public string ManifestJson { get; set; } = null!;
        public OperationStatus Status { get; set; }
        public ICollection<CampaignTarget> Targets { get; set; } = new List<CampaignTarget>();
        public OutBoxDeviceMessage OutBoxDeviceMessage { get; set; }
        public Guid OutBoxDeviceMessageId { get; set; }

        public static Campaign createCampaign(OperationType type,Guid MessageId, string Title,Guid? UserId)
        {
            return new Campaign
            {
                Id = Guid.NewGuid(),
                Deleted = false,
                CreateDate = DateTime.Now,
                OutBoxDeviceMessageId = MessageId,
                Title = Title,
                OperationType = type,
                Status = OperationStatus.Queued,
                CreatedByUserId=UserId,
            };
        }
        
        public void CompeletedCampaign()
        {
            Status = OperationStatus.Completed;
            ModifiedDate = DateTime.Now;  
        }
        public void FiledCampaign()
        {
            Status = OperationStatus.Failed;
            ModifiedDate = DateTime.Now;
        }
        public void NewTarget(CampaignTarget target)=> Targets.Add(target);
    }
}
