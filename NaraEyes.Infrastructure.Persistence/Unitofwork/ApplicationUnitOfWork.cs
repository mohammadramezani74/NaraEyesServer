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
    public partial class ApplicationUnitOfWork(IDbContextFactory<ApplicationDbContext> contextFactory)
    : IApplicationUnitOfWork
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory= contextFactory;
        private ApplicationDbContext? _context;
        private ApplicationDbContext Context
    => _context ??= _contextFactory.CreateDbContext();


        public async Task<OperationResult> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var op = new OperationResult();
            try
            {
                await Context.SaveChangesAsync(cancellationToken);


                return op.succedded();
            }
            catch (DbUpdateConcurrencyException)
            {
                return op.Failed(
                    "این رکورد توسط کاربر یا سرویس دیگری تغییر کرده است. " +
                    "لطفاً صفحه را تازه کنید و دوباره تلاش کنید.");
            }
            catch (DbUpdateException e)
            {
                return op.Failed(e.Message);
            }
        }

        public async Task<int> ExecuteDeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await Context.Set<T>()
                .Where(predicate)
                .ExecuteDeleteAsync();
        }
        public void Dispose()
        {
            if (_context is not null)
            {
                _context.Dispose();
                _context = null;
            }

            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_context is not null)
            {
                await _context.DisposeAsync();
                _context = null;
            }
        }
    }
}
