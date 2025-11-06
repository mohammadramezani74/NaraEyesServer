using ClosedXML.Excel;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Identity;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Base;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Entities.BulkOperation.Enums;
using NaraEyes.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Base
{
    public sealed class BranchService(IApplicationUnitOfWork uow,
        IApplicationUserManager userManamager,
         AuthenticationStateProvider auth) : IBranchService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManamager = userManamager;
        private readonly AuthenticationStateProvider auth = auth;

        public async Task<bool> AddBranchWithExcel(IBrowserFile file, CancellationToken cancellationToken = default)
        {
            if (file is null)
                return false;

            await using var stream = file.OpenReadStream(5 * 1024 * 1024, cancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
                return false;

            var range = ws.RangeUsed();
            if (range == null)
                return false;

            // همه کدهای موجود
            var Supervisions = await _uow.SupervisionStates
                .AsNoTracking()
                .Select(x =>new { x.Code, x.Id })
                .ToListAsync(cancellationToken);
            var branches=await _uow.Branches.AsNoTracking()
                .Select(x=>x.Code).ToListAsync(cancellationToken);
            var existingSet = new HashSet<int>(branches);

            var userId = await GetUserId();
            if (userId == null || userId == Guid.Empty)
                return false;

            var newEntities = new List<Branch>();
            var codesInFile = new HashSet<int>();

            foreach (var row in range.RowsUsed().Skip(1))
            {

                var codeCell = row.Cell(1);
                string codeText;

                if (codeCell.DataType == XLDataType.Number)
                {

                    codeText = ((int)codeCell.GetDouble()).ToString();
                }
                else
                {
                    codeText = codeCell.GetString()?.Trim();
                }


    

                var nameText = row.Cell(2).GetString()?.Trim();
                var shortNameText = row.Cell(3).GetString()?.Trim();
                var SupervisionCode = row.Cell(4).GetString()?.Trim();
                if(SupervisionCode is null)
                {
                    return false;
                }
                var supervisionId= Supervisions.Where(x=>x.Code==int.Parse(SupervisionCode)).Select(x=>x.Id).FirstOrDefault();
                var PostalCode = row.Cell(5).GetString()?.Trim();
                var PhoneNumber = row.Cell(6).GetString()?.Trim();
                var address = row.Cell(7).GetString()?.Trim();
                var Latitude = row.Cell(8).GetString()?.Trim();
                var Longtitude = row.Cell(9).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(codeText))
                    continue;

                if (!int.TryParse(codeText, out var code))
                    continue;

                if (string.IsNullOrWhiteSpace(nameText))
                    continue;


                if (existingSet.Contains(code))
                    continue;


                if (!codesInFile.Add(code))
                    continue;

                var entity = Branch.Create(
                   nameText,
                     code,
                  supervisionId,
                  userId.Value,
                  shortNameText,
                  address,
                  PostalCode,
                  PhoneNumber,
                  !string.IsNullOrEmpty(Latitude)?decimal.Parse(Latitude):0,
                  !string.IsNullOrEmpty(Longtitude)?decimal.Parse(Longtitude):0



                );

                newEntities.Add(entity);
            }

            if (newEntities.Count == 0)
                return false;

            _uow.Branches.AddRange(newEntities);
            await _uow.SaveChangesAsync(cancellationToken);
            return true;
        }

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

            var userId= await GetUserId();
            var newbranch=  Branch.Create(command.Name, command.Code,
                command.SupervisionId, userId.Value,
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

        public async Task<string?> GetBranchReport(CancellationToken cancellationToken = default)
        {

            var list = await _uow.Branches
                .Include(x=>x.Supervision)
            .AsNoTracking()
            .Select(x => new
            {
                x.Code,
                x.Name,
                x.ShortName,
               Supervision= x.Supervision.ShortName,
               x.PostalCode,
               x.Phone,
               x.Address,
               x.Latitude,
                x.Longitude
            })
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Supervisions");

            ws.Cell(1, 1).Value = "کد شعبه";
            ws.Cell(1, 2).Value = "نام";
            ws.Cell(1, 3).Value = "نام نمایشی";
            ws.Cell(1, 4).Value = "سرپرستی";
            ws.Cell(1, 5).Value = "کد پستی";
            ws.Cell(1, 6).Value = "تلفن";
            ws.Cell(1, 7).Value = "آدرس";
            ws.Cell(1, 8).Value = "عرض چغرافیایی";
            ws.Cell(1, 9).Value = "طول چغرافیایی";

            ws.Range("A1:I1").Style.Font.Bold = true;

            var row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.Code;
                ws.Cell(row, 2).Value = item.Name;
                ws.Cell(row, 3).Value = item.ShortName;
                ws.Cell(row, 4).Value = item.Supervision;
                ws.Cell(row, 5).Value = item.PostalCode;
                ws.Cell(row, 6).Value = item.Phone;
                ws.Cell(row, 7).Value = item.Address;
                ws.Cell(row, 8).Value = item.Latitude;
                ws.Cell(row, 9).Value = item.Longitude;
                row++;
            }


            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        public async Task<string?> GetSampleFileForDownload(CancellationToken cancellationToken = default)
        {

            using var wb = new XLWorkbook();

      
            var ws = wb.Worksheets.Add("شعبه ها");
            ws.Cell(1, 1).Value = "کد";
            ws.Cell(1, 2).Value = "نام شعبه";
            ws.Cell(1, 3).Value = "نام نمایشی";
            ws.Cell(1, 4).Value = "کد سرپرستی";
            ws.Cell(1, 5).Value = "کد پستی";
            ws.Cell(1, 6).Value = "تلفن شعبه";
            ws.Cell(1, 7).Value = "آدرس شعبه";
            ws.Cell(1, 8).Value = "عرض جغرافیایی";
            ws.Cell(1, 9).Value = "طول جغرافیایی";


            var headerRange = ws.Range("A1:I1");
            headerRange.Style.Font.Bold = true;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return await Task.FromResult(base64);
        }

        public async Task<OperationResult> UpdateBranchAsync(UpdateBranchModel command, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var userId= await GetUserId(); 
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
                     userId.Value,
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
        private async Task<Guid?> GetUserId()
        {
            var userId = _userManamager.UserId;
            var state = await auth.GetAuthenticationStateAsync();
            var user = state.User;
            var idValue = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(idValue, out var gid))
                userId = gid;
            else
                userId = null;
            var type = OperationType.FileSend;
            return userId;
        }

    }
}
