using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Infrastructure.Persistence.Unitofwork
{
    public partial class ApplicationUnitOfWork(ApplicationDbContext applicationDbContext)
    : IApplicationUnitOfWork
    {
        private readonly ApplicationDbContext _context = applicationDbContext;



        public async Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            try
            {
                await _context.SaveChangesAsync(cancellationToken);


                return op.succedded();
            }
            catch (DbUpdateConcurrencyException e)
            {

                return op.Failed(e.Message);
            }
            catch (DbUpdateException e)
            {
                return op.Failed(e.Message);
            }
        }

        public async Task<int> ExecuteDeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>()
                .Where(predicate)
                .ExecuteDeleteAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
