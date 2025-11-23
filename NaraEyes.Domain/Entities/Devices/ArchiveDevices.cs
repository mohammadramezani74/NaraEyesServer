using NaraEyes.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Devices
{
    public sealed class ArchivedDevice:BaseEntity
    {
        public Device Device { get; set; }
        public Guid DeviceId { get; set; }
        public string ArchiveReason { get; private set; } = null!;

        public static ArchivedDevice CreateArchive(Guid deviceId, Guid createById, string reason)
      => new ArchivedDevice
      {
          Id = Guid.NewGuid(),
          DeviceId = deviceId,
          Deleted = false,
          CreateDate = DateTime.Now,
          CreatedByUserId = createById,
          ArchiveReason = reason,
      };
        public void Restore(Guid modifyById)
        {
            Deleted = true;
            ModifiedById = modifyById;
            this.ModifiedDate= DateTime.Now;
        }
        public void ArchivedAgain(Guid modifyById,string reason)
        {
            Deleted = false;
            ModifiedById = modifyById;
            this.ModifiedDate = DateTime.Now;
            this.ArchiveReason = reason;
        }

    }
}
