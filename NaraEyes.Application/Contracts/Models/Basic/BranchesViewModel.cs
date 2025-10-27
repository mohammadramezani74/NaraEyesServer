using NaraEyes.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public sealed class BranchesViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ShortName { get;  set; }
        public int Code { get;  set; }
        public Guid SupervisionId { get;  set; }
        public string Supervision { get; set; }
        public string? Address { get;  set; }
        public string? PostalCode { get;  set; }
        public string? Phone { get;  set; }
        public decimal? Latitude { get;  set; }
        public decimal? Longitude { get;  set; }
        public bool IsActive { get;  set; } = true;
    }
}
