using NaraEyes.Application.Contracts.Models.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace NaraEyes.Application.Abstraction.Unitofwork
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        public Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
