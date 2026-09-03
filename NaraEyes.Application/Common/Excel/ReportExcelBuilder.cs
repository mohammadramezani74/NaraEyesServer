using ClosedXML.Excel;
using System.Globalization;

namespace NaraEyes.Application.Common.Excel
{
    /// <summary>
    /// سازنده‌ی خروجی اکسل با ظاهر یکدست برای همه‌ی گزارش‌ها.
    ///
    /// یک بار نوشته می‌شود، همه‌ی گزارش‌ها از آن استفاده می‌کنند — تا
    /// خروجی‌ها ظاهر یکسانی داشته باشند و هر گزارش استایل خودش را اختراع نکند.
    ///
    /// نمونه:
    ///     var xl = new ReportExcelBuilder("خرابی قطعات");
    ///     xl.AddTitle("گزارش خرابی قطعات", "از ۱۴۰۴/۰۸/۰۱ تا ۱۴۰۴/۰۸/۳۰");
    ///     xl.AddSummary(new[] { ("کل خرابی‌ها", "۴۷"), ("مجموع زمان", "۱۲۳ ساعت") });
    ///     xl.AddHeader("دستگاه", "شعبه", "ماژول", "تعداد");
    ///     xl.AddRow("10.1.2.3", "مرکزی", "دیسپنسر", 3);
    ///     xl.Finish();
    ///     byte[] bytes = xl.ToBytes();
    /// </summary>
    public sealed class ReportExcelBuilder : IDisposable
    {
        // ---- پالت رنگ ----
        private static readonly XLColor HeaderBg = XLColor.FromHtml("#1B3A8C");
        private static readonly XLColor HeaderFg = XLColor.White;
        private static readonly XLColor TitleFg = XLColor.FromHtml("#0B1437");
        private static readonly XLColor SubtitleFg = XLColor.FromHtml("#5A6274");
        private static readonly XLColor ZebraBg = XLColor.FromHtml("#EEF2FB");
        private static readonly XLColor BorderCol = XLColor.FromHtml("#C9D3E8");
        private static readonly XLColor SummaryBg = XLColor.FromHtml("#F5F8FF");

        private static readonly XLColor SeverityErr = XLColor.FromHtml("#FFE0DE");
        private static readonly XLColor SeverityWarn = XLColor.FromHtml("#FFF4E0");
        private static readonly XLColor SeverityOk = XLColor.FromHtml("#E9F7F1");

        private const string FontName = "Tahoma";

        private readonly XLWorkbook _wb;
        private readonly IXLWorksheet _ws;

        private int _row = 1;
        private int _headerRow = -1;
        private int _firstDataRow = -1;
        private int _colCount;

        public ReportExcelBuilder(string sheetName)
        {
            _wb = new XLWorkbook();
            _ws = _wb.Worksheets.Add(Sanitize(sheetName));

            // فارسی = راست‌به‌چپ
            _ws.RightToLeft = true;
            _ws.Style.Font.FontName = FontName;
            _ws.Style.Font.FontSize = 10;
        }

        // =============================================================
        //  عنوان
        // =============================================================

        public ReportExcelBuilder AddTitle(string title, string? subtitle = null)
        {
            var c = _ws.Cell(_row, 1);
            c.Value = title;
            c.Style.Font.Bold = true;
            c.Style.Font.FontSize = 15;
            c.Style.Font.FontColor = TitleFg;
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            _row++;

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var s = _ws.Cell(_row, 1);
                s.Value = subtitle;
                s.Style.Font.FontSize = 9;
                s.Style.Font.FontColor = SubtitleFg;
                _row++;
            }

            // تاریخ تولید گزارش
            var d = _ws.Cell(_row, 1);
            d.Value = $"تاریخ تهیه: {ToJalali(DateTime.Now)}";
            d.Style.Font.FontSize = 8;
            d.Style.Font.FontColor = SubtitleFg;
            _row += 2;

            return this;
        }

        // =============================================================
        //  کارت‌های خلاصه
        // =============================================================

        /// <summary>یک ردیف از شاخص‌های کلیدی بالای گزارش</summary>
        public ReportExcelBuilder AddSummary(IEnumerable<(string Label, string Value)> items)
        {
            int col = 1;
            int labelRow = _row;
            int valueRow = _row + 1;

            foreach (var (label, value) in items)
            {
                var l = _ws.Cell(labelRow, col);
                l.Value = label;
                l.Style.Font.FontSize = 9;
                l.Style.Font.FontColor = SubtitleFg;
                l.Style.Fill.BackgroundColor = SummaryBg;
                l.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var v = _ws.Cell(valueRow, col);
                v.Value = value;
                v.Style.Font.Bold = true;
                v.Style.Font.FontSize = 12;
                v.Style.Fill.BackgroundColor = SummaryBg;
                v.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                _ws.Range(labelRow, col, valueRow, col).Style
                   .Border.OutsideBorder = XLBorderStyleValues.Thin;
                _ws.Range(labelRow, col, valueRow, col).Style
                   .Border.OutsideBorderColor = BorderCol;

                col++;
            }

            _row = valueRow + 2;
            return this;
        }

        // =============================================================
        //  سربرگ جدول
        // =============================================================

        public ReportExcelBuilder AddHeader(params string[] columns)
        {
            _headerRow = _row;
            _colCount = columns.Length;

            for (int i = 0; i < columns.Length; i++)
            {
                var c = _ws.Cell(_row, i + 1);
                c.Value = columns[i];
                c.Style.Font.Bold = true;
                c.Style.Font.FontColor = HeaderFg;
                c.Style.Fill.BackgroundColor = HeaderBg;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                c.Style.Border.OutsideBorderColor = BorderCol;
            }

            _ws.Row(_row).Height = 22;
            _row++;
            _firstDataRow = _row;

            return this;
        }

        // =============================================================
        //  سطرها
        // =============================================================

        /// <summary>یک سطر معمولی</summary>
        public ReportExcelBuilder AddRow(params object?[] values) => AddRow(null, values);

        /// <summary>
        /// یک سطر با رنگ پس‌زمینه‌ی متناسب با شدت.
        /// null = بدون رنگ (فقط راه‌راه معمولی)
        /// </summary>
        public ReportExcelBuilder AddRow(RowTone? tone, params object?[] values)
        {
            bool zebra = (_row - _firstDataRow) % 2 == 1;

            XLColor bg = tone switch
            {
                RowTone.Error => SeverityErr,
                RowTone.Warning => SeverityWarn,
                RowTone.Ok => SeverityOk,
                _ => zebra ? ZebraBg : XLColor.White,
            };

            for (int i = 0; i < values.Length; i++)
            {
                var c = _ws.Cell(_row, i + 1);
                SetValue(c, values[i]);

                c.Style.Fill.BackgroundColor = bg;
                c.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                c.Style.Border.OutsideBorderColor = BorderCol;
                c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // اعداد وسط‌چین، متن راست‌چین
                c.Style.Alignment.Horizontal = values[i] is int or long or double or decimal
                    ? XLAlignmentHorizontalValues.Center
                    : XLAlignmentHorizontalValues.Right;
            }

            _row++;
            return this;
        }

        private static void SetValue(IXLCell c, object? v)
        {
            switch (v)
            {
                case null:
                    c.Value = "—";
                    break;
                case int i:
                    c.Value = i;
                    c.Style.NumberFormat.Format = "#,##0";
                    break;
                case long l:
                    c.Value = l;
                    c.Style.NumberFormat.Format = "#,##0";
                    break;
                case double d:
                    c.Value = d;
                    c.Style.NumberFormat.Format = "#,##0.0";
                    break;
                case decimal m:
                    c.Value = m;
                    c.Style.NumberFormat.Format = "#,##0";
                    break;
                case bool b:
                    c.Value = b ? "بله" : "خیر";
                    break;
                case DateTime dt:
                    c.Value = ToJalali(dt, withTime: true);
                    break;
                default:
                    c.Value = v.ToString();
                    break;
            }
        }

        // =============================================================
        //  پایان
        // =============================================================

        /// <summary>فریز سربرگ، فیلتر خودکار و تنظیم عرض ستون‌ها</summary>
        public ReportExcelBuilder Finish()
        {
            if (_headerRow > 0 && _row > _firstDataRow)
            {
                // فیلتر خودکار روی سربرگ
                _ws.Range(_headerRow, 1, _row - 1, _colCount).SetAutoFilter();

                // سربرگ همیشه دیده شود
                _ws.SheetView.FreezeRows(_headerRow);
            }

            _ws.Columns().AdjustToContents();

            // جلوگیری از ستون‌های خیلی باریک یا خیلی پهن
            foreach (var col in _ws.ColumnsUsed())
            {
                if (col.Width < 12) col.Width = 12;
                if (col.Width > 45) col.Width = 45;
            }

            // تنظیمات چاپ
            _ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            _ws.PageSetup.FitToPages(1, 0);
            if (_headerRow > 0)
                _ws.PageSetup.SetRowsToRepeatAtTop(_headerRow, _headerRow);

            return this;
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            _wb.SaveAs(ms);
            return ms.ToArray();
        }

        public string ToBase64() => Convert.ToBase64String(ToBytes());

        public void Dispose() => _wb.Dispose();

        // =============================================================
        //  کمکی
        // =============================================================

        public static string ToJalali(DateTime? d, bool withTime = false)
        {
            if (d is null) return "—";

            try
            {
                var pc = new PersianCalendar();
                var v = d.Value;

                string date = $"{pc.GetYear(v):0000}/{pc.GetMonth(v):00}/{pc.GetDayOfMonth(v):00}";
                return withTime ? $"{date} {v:HH:mm}" : date;
            }
            catch
            {
                return d.Value.ToString("yyyy/MM/dd");
            }
        }

        /// <summary>مدت به ثانیه را به متن فارسی خوانا تبدیل می‌کند</summary>
        public static string FormatDuration(long seconds)
        {
            if (seconds < 60) return $"{seconds} ثانیه";
            if (seconds < 3600) return $"{seconds / 60} دقیقه";

            if (seconds < 86400)
            {
                long h = seconds / 3600, m = (seconds % 3600) / 60;
                return m > 0 ? $"{h} ساعت و {m} دقیقه" : $"{h} ساعت";
            }

            long days = seconds / 86400, hours = (seconds % 86400) / 3600;
            return hours > 0 ? $"{days} روز و {hours} ساعت" : $"{days} روز";
        }

        /// <summary>نام شیت نمی‌تواند شامل کاراکترهای خاص باشد</summary>
        private static string Sanitize(string name)
        {
            foreach (char c in new[] { '\\', '/', '*', '?', ':', '[', ']' })
                name = name.Replace(c, '-');

            return name.Length > 31 ? name[..31] : name;
        }
    }

    /// <summary>رنگ پس‌زمینه‌ی سطر بر اساس اهمیت</summary>
    public enum RowTone
    {
        Error,
        Warning,
        Ok,
    }
}