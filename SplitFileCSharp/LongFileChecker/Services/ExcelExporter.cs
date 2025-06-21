using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using LongFileChecker.Models;

namespace LongFileChecker.Services
{
    public class ExcelExporter
    {
        public async Task ExportToExcelAsync(string outputPath, List<FileData> files)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Kết quả");
                WriteHeader(worksheet);
                WriteData(worksheet, files);
                FormatWorksheet(worksheet);

                await package.SaveAsAsync(new FileInfo(outputPath));
            }
        }

        private void WriteHeader(ExcelWorksheet worksheet)
        {
            worksheet.Cells[1, 1].Value = "STT";
            worksheet.Cells[1, 2].Value = "Tên class";
            worksheet.Cells[1, 3].Value = "Tên file";
            worksheet.Cells[1, 4].Value = "Tên hàm";
            worksheet.Cells[1, 5].Value = "Kích thước (ký tự)";
            worksheet.Cells[1, 6].Value = "Access Modifier";
            worksheet.Cells[1, 7].Value = "Return Type";
            worksheet.Cells[1, 8].Value = "Parameters";
            worksheet.Cells[1, 9].Value = "Không sử dụng";
            worksheet.Cells[1, 10].Value = "Là phương thức";

            using (var range = worksheet.Cells[1, 1, 1, 10])
            {
                range.Style.Font.Bold = true;
            }
        }

        private void WriteData(ExcelWorksheet worksheet, List<FileData> files)
        {
            int row = 2;
            int stt = 1;

            foreach (var file in files)
            {
                if (file.CodeItems != null && file.CodeItems.Any())
                {
                    // Sắp xếp các CodeItems theo Length giảm dần
                    var sortedItems = file.CodeItems.OrderByDescending(x => x.Length);
                    foreach (var item in sortedItems)
                    {
                        WriteRow(worksheet, row, stt++, file, item);
                        row++;
                    }
                }
                else
                {
                    worksheet.Cells[row, 1].Value = stt++;
                    worksheet.Cells[row, 3].Value = file.Path;
                    worksheet.Cells[row, 5].Value = file.Length;
                    row++;
                }
            }
        }

        private void WriteRow(ExcelWorksheet worksheet, int row, int stt, FileData file, CodeItemLongFile item)
        {
            worksheet.Cells[row, 1].Value = stt;
            worksheet.Cells[row, 3].Value = file.Path;

            if (item.Type == "Class")
            {
                worksheet.Cells[row, 2].Value = item.Name;
                worksheet.Cells[row, 6].Value = item.AccessModifier;

                if (item.AccessModifier.Contains("private"))
                {
                    worksheet.Cells[row, 9].Value = !item.IsUsed ? "x" : "";
                }
            }
            else if (item.Type == "Method")
            {
                var parts = item.Name.Split('.');
                worksheet.Cells[row, 2].Value = parts[0];
                worksheet.Cells[row, 4].Value = parts[1];
                worksheet.Cells[row, 6].Value = item.AccessModifier;
                worksheet.Cells[row, 7].Value = item.ReturnType;
                worksheet.Cells[row, 8].Value = item.Parameters;
                worksheet.Cells[row, 10].Value = "x";

                if (item.AccessModifier.Contains("private"))
                {
                    worksheet.Cells[row, 9].Value = !item.IsUsed ? "x" : "";
                }
            }

            worksheet.Cells[row, 5].Value = item.Length;
        }

        private void FormatWorksheet(ExcelWorksheet worksheet)
        {
            for (int col = 1; col <= 10; col++)
            {
                worksheet.Column(col).AutoFit();
            }

            worksheet.Column(5).Style.Numberformat.Format = "#,##0";
            worksheet.Column(9).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Column(10).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        }

        public void ExportUnusedItemsPreview(List<UnusedItemInfo> items, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Hoặc Commercial nếu bạn có license

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("UnusedItemsPreview");

                // Header
                int col = 1;
                worksheet.Cells[1, col++].Value = "Selected";
                worksheet.Cells[1, col++].Value = "File Name";
                worksheet.Cells[1, col++].Value = "Class Name";
                worksheet.Cells[1, col++].Value = "Item Name";
                worksheet.Cells[1, col++].Value = "Type";
                worksheet.Cells[1, col++].Value = "Length";
                worksheet.Cells[1, col++].Value = "Access Modifier";
                worksheet.Cells[1, col++].Value = "Full File Path"; // Thêm cột này để có đường dẫn đầy đủ

                using (var range = worksheet.Cells[1, 1, 1, col-1])
                {
                    range.Style.Font.Bold = true;
                }

                // Data
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    col = 1;
                    worksheet.Cells[i + 2, col++].Value = item.IsSelected ? "Yes" : "No";
                    worksheet.Cells[i + 2, col++].Value = Path.GetFileName(item.FilePath);
                    worksheet.Cells[i + 2, col++].Value = item.ClassName;
                    worksheet.Cells[i + 2, col++].Value = item.ItemName;
                    worksheet.Cells[i + 2, col++].Value = item.Type;
                    worksheet.Cells[i + 2, col++].Value = item.Length;
                    worksheet.Cells[i + 2, col++].Value = item.AccessModifier;
                    worksheet.Cells[i + 2, col++].Value = item.FilePath;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                package.Save(); // Lưu lại package
            }
        }
    }
}