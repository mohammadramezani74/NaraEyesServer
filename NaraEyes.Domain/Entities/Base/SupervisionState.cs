using NaraEyes.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Domain.Entities.Base
{
    public class SupervisionState : BaseEntity
    {
        public SupervisionState()
        {
            
        }
        private SupervisionState(string name, int code, string shortName,Guid CreatebyId)
        {
            Name = name;
            Code = code;
            ShortName = shortName;
            CreatedByUserId = CreatebyId;
      
        }
        public string Name { get;private set; }
        public int Code { get;private set; }
        public string? ShortName { get;private set; }
        public ICollection<Branch> Branches { get;private set; } = new List<Branch>();
        public static SupervisionState Create(string name, int code, string shortName,Guid userId)
            => new SupervisionState(name,code,shortName, userId);
        public void update(string name, int code, string shortName, Guid userId)
        {this.Name = name; this.Code = code;
            this.ShortName = shortName;
            ModifiedById = userId;
            ModifiedDate = DateTime.Now;

        }

    }
}
