using NaraEyes.Domain.Entities.Identity;
using NaraEyes.SharedKernel;


namespace NaraEyes.Domain.Common
{
    public class BaseEntity : IAuditableEntity
    {
        public BaseEntity()
        {
            this.Id = Guid.NewGuid();
            this.Deleted = false;
            CreateDate = DateTime.Now;
        }

        public Guid Id { get; set; }


        public bool Deleted { get; set; }

        public DateTime CreateDate { get; set; }


        public Guid? CreatedByUserId { get; set; }


        public User? CreatedByUser { get; set; }


        public DateTime? ModifiedDate { get; set; }


        public Guid? ModifiedById { get; set; }


        public User? ModifiedBy { get; set; }


    }
}
