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
using System.ComponentModel.DataAnnotations;
using Rel = Microsoft.EntityFrameworkCore.RelationalQueryableExtensions;

namespace NaraEyes.Application.Services.Base
{
    public class SupervisionStateService(IApplicationUnitOfWork uow,IApplicationUserManager usermanager, AuthenticationStateProvider auth) : ISupervisionStateService
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;
        private readonly AuthenticationStateProvider auth = auth;

        public async Task<bool> AddSupervisionWithExcel(IBrowserFile file, CancellationToken cancellationToken = default)
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
            var existingCodes = await _uow.SupervisionStates
                .AsNoTracking()
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);
            var existingSet = new HashSet<int>(existingCodes);

            var userId = await GetUserId();
            if (userId == null || userId == Guid.Empty)
                return false;

            var newEntities = new List<SupervisionState>();
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


                //codeText = ToEnglishDigits(codeText);

                var nameText = row.Cell(2).GetString()?.Trim();
                var shortNameText = row.Cell(3).GetString()?.Trim();

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

                var entity = SupervisionState.Create(
                    name: nameText,
                    code: code,
                    shortName: shortNameText,
                    userId: userId.Value
                );

                newEntities.Add(entity);
            }

            if (newEntities.Count == 0)
                return false;

            _uow.SupervisionStates.AddRange(newEntities);
            await _uow.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<string?> GetSampleFileForDownload(CancellationToken cancellationToken = default)
        {
            using var wb = new XLWorkbook();

            // فقط همون شیت اصلی
            var ws = wb.Worksheets.Add("Supervisions");
            ws.Cell(1, 1).Value = "Code";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "ShortName";

            // کمی فرمت تا کاربر بفهمه هدره (اختیاری ولی ارزونه)
            var headerRange = ws.Range("A1:C1");
            headerRange.Style.Font.Bold = true;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return await Task.FromResult(base64);
        }


        public async Task<OperationResult> CreateAsync(CreateSupervisionStateViewModel model, CancellationToken cancellationToken = default)
        {
            var codeExist = await _uow.SupervisionStates.AsNoTracking()
      .AnyAsync(x =>  x.Code == model.Code);

            if (codeExist)
                return new OperationResult().Failed($"کد {model.Code} قبلاً ثبت شده است.");
            var userId = await GetUserId();
            var newSupervisionstate = SupervisionState.Create(model.Name, model.Code, model.ShortName, userId.Value);
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
        private async Task<Guid?> GetUserId()
        {
            var userId = _usermanager.UserId;
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

        public async Task<string?> GetSupervisionReport(CancellationToken cancellationToken = default)
        {
            var list = await _uow.SupervisionStates
          .AsNoTracking()
          .Select(x => new
          {
              x.Code,
              x.Name,
              x.ShortName
          })
          .OrderBy(x => x.Code)
          .ToListAsync(cancellationToken);

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Supervisions");

            ws.Cell(1, 1).Value = "Code";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "ShortName";
            ws.Range("A1:C1").Style.Font.Bold = true;

            var row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.Code;
                ws.Cell(row, 2).Value = item.Name;
                ws.Cell(row, 3).Value = item.ShortName;
                row++;
            }


            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }
    }
}
