using NaraEyes.Domain.Common;

namespace NaraEyes.Domain.Entities.BulkOperation
{
    public class CampaignTarget : BaseEntity
    {
        public Guid CampaignId { get; private set; }
        public Campaign Campaign { get; private set; }
        public string DeviceIp { get; private set; } = null!;
        public bool IsProccessed { get;private  set; }
        public bool IsSuccess { get; set; }

        public static CampaignTarget CreateNewTarget(Guid CampaignId, string Ip, Guid? UserId) =>
            new CampaignTarget
            {
                Id = Guid.NewGuid(),
                Deleted = false,
                CreateDate = DateTime.Now,
                CampaignId = CampaignId,
                DeviceIp = Ip,
                CreatedByUserId = UserId,
                IsProccessed = false,

            };
        public void ProccessdAt()
        {
            IsProccessed = true;
            ModifiedDate = DateTime.Now;
               
        }
    }
}
