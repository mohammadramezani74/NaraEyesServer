using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.EntityFrameworkCore;
using NaraEyes.Application.Abstraction.Unitofwork;
using NaraEyes.Application.Contracts.Interfaces.Reports;
using NaraEyes.Application.Contracts.Models.Basic;
using NaraEyes.Application.Contracts.Models.Devices;
using NaraEyes.Application.Contracts.Models.Reports;
using NaraEyes.Application.Contracts.Utilities;
using NaraEyes.Domain.Entities.Base;
using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Services.Reports
{
    public sealed class ReportService(IApplicationUnitOfWork uow) : IReportService
    {
        private readonly IApplicationUnitOfWork _uow=uow;
        public async Task<PageResultDto<UserActivityReport>> GetUserReports(ReportFilterModel filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 200);
            var usersQuery = _uow.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(filter.Search))
            {
                usersQuery = usersQuery.Where(x => x.FirstName.Contains(filter.Search) ||
                x.LastName.Contains(filter.Search));
            }
            var entities = await usersQuery.OrderByDescending(x => x.LastLoginDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);


            var total = await usersQuery.CountAsync(cancellationToken);
            try
            {

         
            var list = entities.Select(x => new UserActivityReport
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                LastCommand = EnumHelper.GetEnumDisplayName(x.LastInstruction),
                LastCommandTime = x.LastInstructionDate != null ? x.LastInstructionDate.Value.ToFarsiFull() : "-",
                LastLoginDate=x.LastLoginDate!=null?x.LastLoginDate.Value.ToFarsiFull() :"-",

            }
                ).ToList();
            return new PageResultDto<UserActivityReport>
            {
                Items = list,
                Total = total
            };
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public async Task<string?> UserReportsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(Search.Search))
            {
                Search.Page = 1;
                Search.PageSize = 1000;
            }
            var report = await GetUserReports(Search, cancellationToken);


            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش فعالیت کاربران");
            int r = 1;
            ws.Cell(r, 1).Value = "نام";
            ws.Cell(r, 2).Value = "نام خانوادگی";
            ws.Cell(r, 3).Value = "تاریخ آخرین ورود";
            ws.Cell(r, 4).Value = "آخرین دستور";
            ws.Cell(r, 5).Value = "تاریخ آخرین دستور";
            var header = ws.Range(r, 1, r, 4);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            r++;
            foreach (var record in report.Items) 
            {

                ws.Cell(r, 1).Value = record.FirstName;
                ws.Cell(r, 2).Value = record.LastName ?? "";
                ws.Cell(r, 3).Value = record.LastLoginDate;

                ws.Cell(r, 4).Value = record.LastCommand;
                ws.Cell(r, 5).Value = record.LastCommandTime;
                r++;
            }
            var used = ws.RangeUsed();
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            used.Style.Font.FontName = "Tahoma"; // فونت رایج برای فارسی
            ws.SheetView.FreezeRows(1);          // فریز هدر
            used.SetAutoFilter();                 // فیلتر روی هدرها
            ws.Columns().AdjustToContents();      // عرض مناسب ستون‌ها

            // تبدیل به Base64 (بدون ذخیره‌ی فیزیکی)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }
        public async Task<PageResultDto<FileUploadReport>> GetFilesReports(ReportFilterModel filter, CancellationToken cancellationToken = default)
        {
            if (filter.StartDate == null)
            {
                filter.StartDate = DateTime.Now;
                filter.EndDate = DateTime.Now;
            }
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 200);
            var filesQuery =  _uow.OutBoxDeviceMessages.AsNoTracking()
               .Include(x => x.Campaign.Targets)
               .Include(x=>x.CreatedByUser)
               .Where(x => x.CommandType == CommandType.UploadGroupFile&&
               x.CreateDate.Date>=filter.StartDate.Value.Date
               && x.CreateDate.Date<= filter.EndDate!.Value.Date)
               .AsQueryable();
            if(!string.IsNullOrEmpty(filter.Search))
            {
                filesQuery = filesQuery.Where(x => x.Payload.Trim().Contains(filter.Search) ||
                x.Campaign.Targets.Any(x => x.DeviceIp.Contains(filter.Search)) ||
                x.CreatedByUser.FirstName.Contains(filter.Search) ||
                   x.CreatedByUser.LastName.Contains(filter.Search));

            }
            var queryList=await filesQuery.ToListAsync();
            var SelectedDevice= queryList.SelectMany(x=>x.Campaign.Targets).ToList();
            var entities =  SelectedDevice.OrderByDescending(x => x.CreateDate)
                .Select(x =>new FileUploadReport
                {
                    Ip=x.DeviceIp,
                    UploadDate=MapUploadDate(queryList,x.CampaignId),
                    FileName=MapFileName(queryList,x.CampaignId),
                    UploadedBy=MapUploadedBy(queryList,x.CampaignId),
                    SaveFile=x.IsSuccess,
                    SaveTime=x.ModifiedDate!=null? x.ModifiedDate.Value.ToShortTimeString():"-"
                    
                    
                })
  .Skip((page - 1) * pageSize)
  .Take(pageSize)
  .ToList();


            var total =  entities.Count;
        
            return new PageResultDto<FileUploadReport>
            {
                Items = entities,
                Total = total
            };

        }

        public async Task<string?> FileUploadsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(Search.Search)&& Search.StartDate==null)
            {
                Search.Page = 1;
                Search.PageSize = 1000;
            }
            var report = await GetFilesReports(Search, cancellationToken);


            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش ارسال فایل گروهی");
            int r = 1;
            ws.Cell(r, 1).Value = "آیپی دستگاه";
            ws.Cell(r, 2).Value = "نام فایل";
            ws.Cell(r, 3).Value = "تاریخ آپلود";
            ws.Cell(r, 4).Value = "آپلود شده توسط";
            ws.Cell(r, 5).Value = "دخیره در دستگاه";
            ws.Cell(r, 6).Value = "زمان ذخیره در دستگاه";

            var header = ws.Range(r, 1, r, 4);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            r++;
            foreach (var record in report.Items)
            {

                ws.Cell(r, 1).Value = record.Ip;
                ws.Cell(r, 2).Value = record.FileName ?? "";
                ws.Cell(r, 3).Value = record.UploadDate;

                ws.Cell(r, 4).Value = record.UploadDate;
                ws.Cell(r, 5).Value = record.SaveFile?"موفق":"شکست خورده";
                ws.Cell(r, 6).Value = record.SaveTime;
                r++;
            }
            var used = ws.RangeUsed();
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            used.Style.Font.FontName = "Tahoma"; // فونت رایج برای فارسی
            ws.SheetView.FreezeRows(1);          // فریز هدر
            used.SetAutoFilter();                 // فیلتر روی هدرها
            ws.Columns().AdjustToContents();      // عرض مناسب ستون‌ها

            // تبدیل به Base64 (بدون ذخیره‌ی فیزیکی)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }

        public async Task<PageResultDto<DeviceRestartReport>> GetRestartReports(ReportFilterModel filter, CancellationToken cancellationToken = default)
        {
            if (filter.StartDate == null)
            {
                filter.StartDate = DateTime.Now;
                filter.EndDate = DateTime.Now;
            }
            var page = filter.Page <= 0 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 200);
            var filesQuery = _uow.OutBoxDeviceMessages.AsNoTracking()
               .Include(x => x.Campaign.Targets)
               .Include(x => x.CreatedByUser)
               .Where(x => x.CommandType == CommandType.ResetGroup &&
               x.CreateDate.Date >= filter.StartDate.Value.Date
               && x.CreateDate.Date <= filter.EndDate!.Value.Date)
               .AsQueryable();
            if (!string.IsNullOrEmpty(filter.Search))
            {
                filesQuery = filesQuery.Where(x => x.Payload.Trim().Contains(filter.Search) ||
                x.Campaign.Targets.Any(x => x.DeviceIp.Contains(filter.Search)) ||
                x.CreatedByUser.FirstName.Contains(filter.Search) ||
                   x.CreatedByUser.LastName.Contains(filter.Search));

            }
            var queryList = await filesQuery.ToListAsync();
            var SelectedDevice = queryList.SelectMany(x => x.Campaign.Targets).ToList();
            var entities = SelectedDevice.OrderByDescending(x => x.CreateDate)
                .Select(x => new DeviceRestartReport
                {
                    Ip = x.DeviceIp,
                    RestartTime = MapUploadDate(queryList, x.CampaignId),
                    RestartedBy = MapFileName(queryList, x.CampaignId),
                    IsSuccess = x.IsSuccess,
                    ResetAt = x.ModifiedDate != null ? x.ModifiedDate.Value.ToShortTimeString() : "-"


                })
  .Skip((page - 1) * pageSize)
  .Take(pageSize)
  .ToList();


            var total = entities.Count;

            return new PageResultDto<DeviceRestartReport>
            {
                Items = entities,
                Total = total
            };

        }
        public async Task<string?> GetRestartReportsExcel(ReportFilterModel Search, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(Search.Search) && Search.StartDate == null)
            {
                Search.Page = 1;
                Search.PageSize = 1000;
            }
            var report = await GetRestartReports(Search, cancellationToken);


            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش ارسال فایل گروهی");
            int r = 1;
            ws.Cell(r, 1).Value = "آیپی دستگاه";
            ws.Cell(r, 2).Value = "زمان ریست گروهی";
            ws.Cell(r, 3).Value = "ریست شده توسط";
            ws.Cell(r, 4).Value = "ریست دستگاه";
            ws.Cell(r, 5).Value = "زمان ریست";


            var header = ws.Range(r, 1, r, 4);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            header.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            header.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            r++;
            foreach (var record in report.Items)
            {

                ws.Cell(r, 1).Value = record.Ip;
                ws.Cell(r, 2).Value = record.ResetAt ?? "";
                ws.Cell(r, 3).Value = record.RestartedBy;

                ws.Cell(r, 4).Value = record.IsSuccess ? "موفق" : "شکست خورده";
                ws.Cell(r, 5).Value = record.RestartTime;
                r++;
            }
            var used = ws.RangeUsed();
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            used.Style.Font.FontName = "Tahoma"; // فونت رایج برای فارسی
            ws.SheetView.FreezeRows(1);          // فریز هدر
            used.SetAutoFilter();                 // فیلتر روی هدرها
            ws.Columns().AdjustToContents();      // عرض مناسب ستون‌ها

            // تبدیل به Base64 (بدون ذخیره‌ی فیزیکی)
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }



        ///---------------------------------------- Private Methodes ----------------------------------\\\
        private static string MapUploadDate(List<OutBoxDeviceMessage> queryList, Guid campaignId)
        {
         var date=queryList.Where(x=>x.Campaign.Id == campaignId).FirstOrDefault();
            if (date != null) {
           return date.CreateDate.ToFarsiFull();
            }
            return "-";

        }
        private static string MapFileName(List<OutBoxDeviceMessage> queryList, Guid campaignId)
        {
            var date = queryList.Where(x => x.Campaign.Id == campaignId).FirstOrDefault();
            if (date != null)
            {
                return date.Payload;
            }
            return "-";

        }
        private static string MapUploadedBy(List<OutBoxDeviceMessage> queryList, Guid campaignId)
        {
            var date = queryList.Where(x => x.Campaign.Id == campaignId).FirstOrDefault();
            if (date != null)
            {
                return date.CreatedByUser.FirstName+" " + date.CreatedByUser.LastName;
            }
            return "-";

        }
    }
}
