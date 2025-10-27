using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using Rel = Microsoft.EntityFrameworkCore.RelationalQueryableExtensions;

namespace NaraEyes.Application.Services.Base
{
    public class SupervisionStateService(IApplicationUnitOfWork uow,IApplicationUserManager usermanager) : ISupervisionStateService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;

        public async Task<OperationResult> CreateAsync(CreateSupervisionStateViewModel model, CancellationToken cancellationToken = default)
        {
            var codeExist = await _uow.SupervisionStates.AsNoTracking()
      .AnyAsync(x =>  x.Code == model.Code);

            if (codeExist)
                return new OperationResult().Failed($"کد {model.Code} قبلاً ثبت شده است.");
            var userId = _usermanager.UserId!.Value;
            var newSupervisionstate = SupervisionState.Create(model.Name, model.Code, model.ShortName, userId);
            _uow.SupervisionStates.Add(newSupervisionstate);
            await _uow.SaveChangesAsync(cancellationToken);
            return new OperationResult().succedded();
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var state = await _uow.SupervisionStates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            _uow.SupervisionStates.Remove(state);
           await _uow.SaveChangesAsync(cancellationToken);

        }

        public async Task<SupervisionStateViewModel> GetSupervisionStateByIdAsync(Guid Id, CancellationToken cancellationToken = default)
        {
            var state = await _uow.SupervisionStates.AsNoTracking()
                  .Select(x => new SupervisionStateViewModel
                  {
                      Id = x.Id,
                      Name = x.Name,
                      Code = x.Code,
                      ShortName = x.ShortName

                  }).FirstOrDefaultAsync(x=>x.Id==Id,cancellationToken);
            return state!;
        }

        public async Task<List<SupervisionStateViewModel>> GetSupervisionStates(CancellationToken cancellationToken = default)
        {
            var AllStates = await _uow.SupervisionStates.AsNoTracking()
                     .Select(x => new SupervisionStateViewModel
                     {
                         Id = x.Id,
                         Code = x.Code,
                         Name = x.Name,
                         ShortName = x.ShortName ?? string.Empty
                     }).ToListAsync();

            return AllStates;
        }

        public async Task<OperationResult> UpdateAsync(SupervisionStateViewModel model, CancellationToken cancellationToken = default)
        {
            var userId = _usermanager.UserId!.Value;
            var op = new OperationResult();
           SupervisionState? state=await _uow.SupervisionStates.FirstOrDefaultAsync(s=>s.Id==model.Id,cancellationToken);
            if (state is null) return op.Failed("سرپرستی پیدا نشد.");
            var codeExist=await _uow.SupervisionStates.AsNoTracking()
                .AnyAsync(x => x.Id != model.Id && x.Code == model.Code);

            if (codeExist)
              return op.Failed($"کد {model.Code} قبلاً ثبت شده است.");
            state.update(model.Name,model.Code,model.ShortName, userId);
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
        }
    }
}
