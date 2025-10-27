using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Base
{
    public sealed class BranchService(IApplicationUnitOfWork uow,
        IApplicationUserManager userManamager) : IBranchService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManamager = userManamager;

        public async Task<OperationResult> CreateBranchAsync(CreateBranchModel command, CancellationToken cancellationToken)
        {

            var op = new OperationResult();
            if (command is null)
                return op.Failed("درخواست نامعتبر است.");
            var supervisionExists = await _uow.SupervisionStates
        .AnyAsync(s => s.Id == command.SupervisionId, cancellationToken);
            if (!supervisionExists)
                return op.Failed("سرپرستی انتخاب‌شده وجود ندارد.");
            var codeExists = await _uow.Branches
         .AnyAsync(b => b.Code == command.Code && b.SupervisionId == command.SupervisionId, cancellationToken);
            if (codeExists)
                return op.Failed("کُد شعبه در این سرپرستی تکراری است.");

            var userId= _userManamager.UserId!.Value;
          var newbranch=  Branch.Create(command.Name, command.Code,
                command.SupervisionId, userId,
                command.ShortName, command.Address, command.PostalCode,
                command.Phone, command.Latitude, command.Longitude);
            _uow.Branches.Add(newbranch);
            try
            {

       
            await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {

               return op.Failed("ثبت شعبه با خطا مواجه شد!");
            }
            return op.succedded();

        }

        public async Task<OperationResult> DeleteBranchAsync(Guid Id, CancellationToken cancellationToken)
        {
            var op=new OperationResult();
           var targetBranche= await _uow.Branches.FirstOrDefaultAsync(b => b.Id == Id,cancellationToken);
            if(targetBranche == null)
            {
               return op.Failed("شعبه ای برای حذف یافت نشد");
            }
            try
            {

                _uow.Branches.Remove(targetBranche);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {

                return op.Failed("حذف شعبه با خطا مواجه شد!");
            }

            return op.succedded();
        }

        public async Task<List<BranchesViewModel>> GetAllBranches(CancellationToken cancellationToken)
        {
           var branches= await _uow.Branches.AsNoTracking()
                .Select(x=>new BranchesViewModel
                {
                    Id = x.Id,
                    ShortName = x.ShortName,
                    Supervision=x.Supervision.Name,
                    SupervisionId=x.SupervisionId,
                    Address=x.Address,
                    Code=x.Code,
                    IsActive=x.IsActive,
                    Latitude=x.Latitude,
                    Longitude=x.Longitude,
                    Name=x.Name,
                    Phone=x.Phone,
                    PostalCode=x.PostalCode,
                }).ToListAsync(cancellationToken);
            return branches;
        }

        public async Task<List<BranchesViewModel>> GetBranchesBySupervisionIdAsync(Guid Id, CancellationToken cancellationToken = default)
        {
            var branches = await _uow.Branches.AsNoTracking()
                .Where(x=>x.SupervisionId == Id)
           .Select(x => new BranchesViewModel
           {
               Id = x.Id,
               ShortName = x.ShortName,
               Name = x.Name,
           }).ToListAsync(cancellationToken);
            return branches;
        }

        public async Task<OperationResult> UpdateBranchAsync(UpdateBranchModel command, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var userId= _userManamager.UserId!.Value;
            if (command is null)
                return op.Failed("درخواست نامعتبر است.");

        
            var branch = await _uow.Branches
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (branch is null)
                return op.Failed("شعبه مورد نظر یافت نشد.");

         
            var supervisionExists = await _uow.SupervisionStates
                .AnyAsync(s => s.Id == command.SupervisionId, cancellationToken);

            if (!supervisionExists)
                return op.Failed("سرپرستی انتخاب‌شده وجود ندارد.");
            var codeTaken = await _uow.Branches
    .AnyAsync(b => b.Id != command.Id
                && b.Code == command.Code
                && b.SupervisionId == command.SupervisionId,
              cancellationToken);
            if (codeTaken)
                return op.Failed("کُد شعبه در این سرپرستی تکراری است.");
            try
            {
             
                if (branch.SupervisionId != command.SupervisionId)
                {
                
                 branch.SetSupervisionId(command.SupervisionId);
                }

                branch.UpdateInfo(
                     userId,
                    name: command.Name,
                    shortName: command.ShortName,
                    address: command.Address,
                    postalCode: command.PostalCode,
                    phone: command.Phone,
                    latitude: command.Latitude,
                    longtitude: command.Longitude
                );

         
                if (command.IsActive) branch.Activate(); else branch.Deactivate();

     
                if (branch.Code != command.Code)
                {
            branch.SetCode(command.Code);
                }

                await _uow.SaveChangesAsync(cancellationToken);
                return op.succedded("تغییرات شعبه با موفقیت ثبت شد.");
            }
            catch (DomainException dx)
            {
      
                return op.Failed(dx.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                return op.Failed("اطلاعات شعبه توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را به‌روزرسانی کنید.");
            }
            catch (DbUpdateException dbx)
            {
                return op.Failed("ذخیره‌سازی شعبه با خطای پایگاه‌داده مواجه شد." );
            }
            catch (Exception)
            {
                return op.Failed("خطای غیرمنتظره‌ای رخ داد.");
            }

        }
    }
}
