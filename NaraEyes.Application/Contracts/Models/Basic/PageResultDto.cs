using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Basic
{
    public sealed class PageResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
    }
}
