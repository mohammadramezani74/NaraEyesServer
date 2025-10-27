using NaraEyes.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaraEyes.Application.Contracts.Models.Devices
{
    public class DeviceFilterViewModel
    {
        /// <summary>عبارت جستجو روی نام، آی‌پی، سریال، شعبه</summary>
        public string? Search { get; set; }

        /// <summary>فیلتر وضعیت</summary>
        public DeviceMode? Status { get; set; }

        /// <summary>فیلتر شعبه (دقیق)</summary>
        public Guid? Branch { get; set; }

        /// <summary>شماره صفحه (۱-مبنا)</summary>
        public int Page { get; set; } = 1;

        /// <summary>تعداد آیتم در هر صفحه</summary>
        public int PageSize { get; set; } = 20;

        /// <summary>برچسب ستون برای سورت (status|name|ip|branch|seen|serial|cash|updated)</summary>
        public string? SortLabel { get; set; }

        /// <summary>جهت سورت</summary>
        public SortDirectionDto SortDirection { get; set; } = SortDirectionDto.None;
    }
    public enum SortDirectionDto
    {
        None = 0,
        Ascending = 1,
        Descending = 2
    }
}
