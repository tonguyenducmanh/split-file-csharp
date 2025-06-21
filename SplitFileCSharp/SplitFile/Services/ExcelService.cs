using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SplitFile.Models;
using System.IO;
using System.Linq;

namespace FileToolLib.Services
{
    public class ExcelService
    {
        private const int COL_ORIGINAL_FILE = 0;
        private const int COL_IS_MAIN = 1;
        private const int COL_MAIN_FILE = 2;
        private const int COL_NEW_FILE = 3;
        private const int COL_DESCRIPTION = 4;
        private const int COL_METHODS = 5;

        /// <summary>
        /// Lấy danh sách sheets từ file Excel
        /// </summary>
        public List<string> GetSheetNames(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = filePath.EndsWith(".xlsx") ?
                new XSSFWorkbook(stream) : new HSSFWorkbook(stream);

            var sheets = new List<string>();
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                sheets.Add(workbook.GetSheetName(i));
            }
            return sheets;
        }

        /// <summary>
        /// Đọc file Excel và chuyển đổi thành danh sách cấu hình
        /// </summary>
        public List<ImportFileConfig> ReadExcel(string filePath, string sheetName)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = filePath.EndsWith(".xlsx") ?
                new XSSFWorkbook(stream) : new HSSFWorkbook(stream);

            var sheet = workbook.GetSheet(sheetName);
            if (sheet == null)
                throw new ArgumentException($"Không tìm thấy sheet '{sheetName}'");

            var result = new List<ImportFileConfig>();
            string? lastOriginalFile = null;
            string? lastMainFile = null;

            // Bỏ qua dòng header
            for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null) continue;

                // Đọc các cell
                var config = new ImportFileConfig();

                // Xử lý file gốc
                var originalFile = GetCellValue(row.GetCell(COL_ORIGINAL_FILE));
                if (string.IsNullOrWhiteSpace(originalFile))
                {
                    if (lastOriginalFile == null) continue;
                    config.OriginalFile = lastOriginalFile;
                }
                else
                {
                    config.OriginalFile = originalFile;
                    lastOriginalFile = originalFile;
                }

                // Xử lý trạng thái file chính
                var isMainValue = GetCellValue(row.GetCell(COL_IS_MAIN));
                config.IsMainFile = isMainValue?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) ?? false;

                // Xử lý file chính
                var mainFile = GetCellValue(row.GetCell(COL_MAIN_FILE));
                if (!config.IsMainFile)
                {
                    if (string.IsNullOrWhiteSpace(mainFile))
                    {
                        if (lastMainFile == null) continue;
                        config.MainFile = lastMainFile;
                    }
                    else
                    {
                        config.MainFile = mainFile;
                        lastMainFile = mainFile;
                    }
                }

                // Đọc các thông tin khác
                config.NewFileName = GetCellValue(row.GetCell(COL_NEW_FILE)) ?? "";
                config.Description = GetCellValue(row.GetCell(COL_DESCRIPTION));
                config.Methods = GetCellValue(row.GetCell(COL_METHODS)) ?? "";

                if (config.IsValid())
                {
                    result.Add(config);
                }
            }

            return result;
        }

        /// <summary>
        /// Tạo file mẫu Excel
        /// </summary>
        public void CreateSampleFile(string filePath)
        {
            IWorkbook workbook = filePath.EndsWith(".xlsx") ?
                (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();

            // Tạo các styles
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
            headerStyle.SetFont(headerFont);
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.RoyalBlue.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var wrappedStyle = workbook.CreateCellStyle();
            wrappedStyle.WrapText = true;

            // Tạo sheet mẫu
            var sampleSheet = workbook.CreateSheet("Sample");
            var headerRow = sampleSheet.CreateRow(0);

            // Tạo header với style
            CreateHeaderCell(headerRow, COL_ORIGINAL_FILE, "Original File", headerStyle);
            CreateHeaderCell(headerRow, COL_IS_MAIN, "Is Main", headerStyle);
            CreateHeaderCell(headerRow, COL_MAIN_FILE, "Main File", headerStyle);
            CreateHeaderCell(headerRow, COL_NEW_FILE, "New File", headerStyle);
            CreateHeaderCell(headerRow, COL_DESCRIPTION, "Description", headerStyle);
            CreateHeaderCell(headerRow, COL_METHODS, "Methods", headerStyle);

            // Set column widths
            sampleSheet.SetColumnWidth(COL_ORIGINAL_FILE, 50 * 256); // 50 characters
            sampleSheet.SetColumnWidth(COL_IS_MAIN, 10 * 256);
            sampleSheet.SetColumnWidth(COL_MAIN_FILE, 50 * 256);
            sampleSheet.SetColumnWidth(COL_NEW_FILE, 20 * 256);
            sampleSheet.SetColumnWidth(COL_DESCRIPTION, 30 * 256);
            sampleSheet.SetColumnWidth(COL_METHODS, 40 * 256);

            // Tạo dữ liệu mẫu
            CreateSampleRow(sampleSheet, 1, new Dictionary<int, string>
            {
                { COL_ORIGINAL_FILE, @"D:\BLBase.cs" },
                { COL_IS_MAIN, "Yes" },
                { COL_NEW_FILE, "Service" },
                { COL_DESCRIPTION, "Service layer methods" },
                { COL_METHODS, "GetById(123)\nGetList(234)\nUpdate(456)" }
            }, wrappedStyle);

            CreateSampleRow(sampleSheet, 2, new Dictionary<int, string>
            {
                { COL_IS_MAIN, "No" },
                { COL_MAIN_FILE, @"D:\BLBase.cs" },
                { COL_NEW_FILE, "Helper" },
                { COL_DESCRIPTION, "Helper methods" },
                { COL_METHODS, "Format, Validate, Process" }
            }, wrappedStyle);

            CreateSampleRow(sampleSheet, 3, new Dictionary<int, string>
            {
                { COL_ORIGINAL_FILE, @"D:\BLBase.cs" },
                { COL_IS_MAIN, "Yes" },
                { COL_NEW_FILE, "Domain" },
                { COL_DESCRIPTION, "Domain logic" },
                { COL_METHODS, "Calculate(789), Compute" }
            }, wrappedStyle);

            // Freeze header row
            sampleSheet.CreateFreezePane(0, 1);

            // Tạo sheet template
            var templateSheet = workbook.CreateSheet("Template");
            var templateHeader = templateSheet.CreateRow(0);

            CreateHeaderCell(templateHeader, COL_ORIGINAL_FILE, "Original File", headerStyle);
            CreateHeaderCell(templateHeader, COL_IS_MAIN, "Is Main", headerStyle);
            CreateHeaderCell(templateHeader, COL_MAIN_FILE, "Main File", headerStyle);
            CreateHeaderCell(templateHeader, COL_NEW_FILE, "New File", headerStyle);
            CreateHeaderCell(templateHeader, COL_DESCRIPTION, "Description", headerStyle);
            CreateHeaderCell(templateHeader, COL_METHODS, "Methods", headerStyle);

            // Set column widths cho template
            templateSheet.SetColumnWidth(COL_ORIGINAL_FILE, 50 * 256);
            templateSheet.SetColumnWidth(COL_IS_MAIN, 10 * 256);
            templateSheet.SetColumnWidth(COL_MAIN_FILE, 50 * 256);
            templateSheet.SetColumnWidth(COL_NEW_FILE, 20 * 256);
            templateSheet.SetColumnWidth(COL_DESCRIPTION, 30 * 256);
            templateSheet.SetColumnWidth(COL_METHODS, 40 * 256);

            // Freeze header row
            templateSheet.CreateFreezePane(0, 1);

            // Add data validation for Yes/No
            var regions = new CellRangeAddressList(1, 1000, COL_IS_MAIN, COL_IS_MAIN);

            if (workbook is XSSFWorkbook)
            {
                var xssfSheet = (XSSFSheet)templateSheet;
                var dvHelper = new XSSFDataValidationHelper(xssfSheet);
                var dvConstraint = dvHelper.CreateExplicitListConstraint(new[] { "Yes", "No" });
                var validation = dvHelper.CreateValidation(dvConstraint, regions);
                validation.CreateErrorBox("Invalid Value", "Please enter either 'Yes' or 'No'");
                validation.ShowErrorBox = true;
                xssfSheet.AddValidationData(validation);
            }
            else if (workbook is HSSFWorkbook)
            {
                var hssfSheet = (HSSFSheet)templateSheet;
                var dvHelper = new HSSFDataValidationHelper(hssfSheet);
                var dvConstraint = dvHelper.CreateExplicitListConstraint(new[] { "Yes", "No" });
                var validation = dvHelper.CreateValidation(dvConstraint, regions);
                validation.CreateErrorBox("Invalid Value", "Please enter either 'Yes' or 'No'");
                validation.ShowErrorBox = true;
                hssfSheet.AddValidationData(validation);
            }

            // Lưu file
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
        }

        /// <summary>
        /// Xuất cấu hình hiện tại ra file Excel
        /// </summary>
        public void ExportConfig(string filePath, List<ImportFileConfig> configs)
        {
            IWorkbook workbook = filePath.EndsWith(".xlsx") ?
                (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();

            // Create styles
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;
            headerStyle.SetFont(headerFont);
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.RoyalBlue.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var wrappedStyle = workbook.CreateCellStyle();
            wrappedStyle.WrapText = true;

            // Create sheet
            var sheet = workbook.CreateSheet("Configuration");
            var headerRow = sheet.CreateRow(0);

            // Create headers
            CreateHeaderCell(headerRow, COL_ORIGINAL_FILE, "Original File", headerStyle);
            CreateHeaderCell(headerRow, COL_IS_MAIN, "Is Main", headerStyle);
            CreateHeaderCell(headerRow, COL_MAIN_FILE, "Main File", headerStyle);
            CreateHeaderCell(headerRow, COL_NEW_FILE, "New File", headerStyle);
            CreateHeaderCell(headerRow, COL_DESCRIPTION, "Description", headerStyle);
            CreateHeaderCell(headerRow, COL_METHODS, "Methods", headerStyle);

            // Set column widths
            sheet.SetColumnWidth(COL_ORIGINAL_FILE, 50 * 256);
            sheet.SetColumnWidth(COL_IS_MAIN, 10 * 256);
            sheet.SetColumnWidth(COL_MAIN_FILE, 50 * 256);
            sheet.SetColumnWidth(COL_NEW_FILE, 20 * 256);
            sheet.SetColumnWidth(COL_DESCRIPTION, 30 * 256);
            sheet.SetColumnWidth(COL_METHODS, 40 * 256);

            // Add data
            for (int i = 0; i < configs.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var config = configs[i];

                CreateCell(row, COL_ORIGINAL_FILE, config.OriginalFile, wrappedStyle);
                CreateCell(row, COL_IS_MAIN, config.IsMainFile ? "Yes" : "No", null);
                CreateCell(row, COL_MAIN_FILE, config.MainFile ?? "", wrappedStyle);
                CreateCell(row, COL_NEW_FILE, config.NewFileName, wrappedStyle);
                CreateCell(row, COL_DESCRIPTION, config.Description ?? "", wrappedStyle);
                CreateCell(row, COL_METHODS, config.Methods, wrappedStyle);
            }

            // Freeze header row
            sheet.CreateFreezePane(0, 1);

            // Add data validation for Yes/No
            var regions = new CellRangeAddressList(1, configs.Count + 1, COL_IS_MAIN, COL_IS_MAIN);

            if (workbook is XSSFWorkbook)
            {
                var xssfSheet = (XSSFSheet)sheet;
                var dvHelper = new XSSFDataValidationHelper(xssfSheet);
                var dvConstraint = dvHelper.CreateExplicitListConstraint(new[] { "Yes", "No" });
                var validation = dvHelper.CreateValidation(dvConstraint, regions);
                validation.CreateErrorBox("Invalid Value", "Please enter either 'Yes' or 'No'");
                validation.ShowErrorBox = true;
                xssfSheet.AddValidationData(validation);
            }
            else if (workbook is HSSFWorkbook)
            {
                var hssfSheet = (HSSFSheet)sheet;
                var dvHelper = new HSSFDataValidationHelper(hssfSheet);
                var dvConstraint = dvHelper.CreateExplicitListConstraint(new[] { "Yes", "No" });
                var validation = dvHelper.CreateValidation(dvConstraint, regions);
                validation.CreateErrorBox("Invalid Value", "Please enter either 'Yes' or 'No'");
                validation.ShowErrorBox = true;
                hssfSheet.AddValidationData(validation);
            }

            // Save file
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
        }

        public void ExportAnalysisResult(AnalizeResult analysisResult, List<AnalyzedMethod> methods, string filePath, string mainAnalyzedFile, Dictionary<string, Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax> classNodes)
        {
            IWorkbook workbook = filePath.EndsWith(".xlsx") ?
                (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();

            var sheet = workbook.CreateSheet("AnalysisResult");

            // Tạo style cho header
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);

            // Tạo dòng header
            var headerRow = sheet.CreateRow(0);
            int colIdx = 0;
            CreateHeaderCell(headerRow, colIdx++, "STT", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Tên class", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Tên file", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Tên hàm", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Kích thước (ký tự)", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Access Modifier", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Return Type", headerStyle);
            CreateHeaderCell(headerRow, colIdx++, "Parameters", headerStyle);

            // Sắp xếp methods: trước tiên theo SourceFileName (A-Z), sau đó theo Length (lớn đến nhỏ)
            var sortedMethods = methods
                .OrderBy(m => m.SourceFileName)
                .ThenByDescending(m => m.Length)
                .ToList();

            // Điền dữ liệu
            for (int i = 0; i < sortedMethods.Count; i++)
            {
                var method = sortedMethods[i];
                var dataRow = sheet.CreateRow(i + 1); // +1 vì dòng 0 là header
                colIdx = 0;

                string className = "";
                // Ưu tiên lấy tên class từ classNodes nếu method đó thuộc về một file đã được parse class riêng
                if (classNodes.TryGetValue(method.SourceFile, out var classSyntax))
                {
                    className = classSyntax.Identifier.Text;
                }
                // Nếu không có trong classNodes (ví dụ method từ file chính), và source file của method trùng với mainAnalyzedFile,
                // thì lấy ClassName từ AnalizeResult (thường là class chính của file main)
                else if (!string.IsNullOrEmpty(analysisResult.ClassName) &&
                         Path.GetFullPath(method.SourceFile).Equals(Path.GetFullPath(mainAnalyzedFile), StringComparison.OrdinalIgnoreCase))
                {
                    className = analysisResult.ClassName;
                }
                // Fallback: Nếu vẫn không có, thử tìm trong classNodes bằng tên file không đường dẫn (ít chính xác hơn)
                else if (classNodes.TryGetValue(method.SourceFileName, out var classSyntaxByNameOnly))
                {
                    className = classSyntaxByNameOnly.Identifier.Text;
                }

                dataRow.CreateCell(colIdx++).SetCellValue(i + 1); // STT
                dataRow.CreateCell(colIdx++).SetCellValue(className);
                dataRow.CreateCell(colIdx++).SetCellValue(method.SourceFileName);
                dataRow.CreateCell(colIdx++).SetCellValue(method.Name);
                dataRow.CreateCell(colIdx++).SetCellValue(method.Length);
                dataRow.CreateCell(colIdx++).SetCellValue(method.AccessModifier);
                dataRow.CreateCell(colIdx++).SetCellValue(method.ReturnType);
                dataRow.CreateCell(colIdx++).SetCellValue(method.Parameters);
            }

            // Tự động điều chỉnh độ rộng cột cho vừa nội dung (tùy chọn, có thể làm chậm với nhiều dữ liệu)
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Freeze header row
            sheet.CreateFreezePane(0, 1);

            // Lưu file
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(fs);
        }

        private void CreateCell(IRow row, int column, string value, ICellStyle? style)
        {
            var cell = row.CreateCell(column);
            cell.SetCellValue(value);
            if (style != null)
            {
                cell.CellStyle = style;
            }
        }

        private void CreateSampleRow(ISheet sheet, int rowIndex, Dictionary<int, string> values, ICellStyle style)
        {
            var row = sheet.CreateRow(rowIndex);
            foreach (var value in values)
            {
                var cell = row.CreateCell(value.Key);
                cell.SetCellValue(value.Value);
                cell.CellStyle = style;
            }
        }

        private void CreateHeaderCell(IRow row, int column, string value, ICellStyle style)
        {
            var cell = row.CreateCell(column);
            cell.SetCellValue(value);
            cell.CellStyle = style;
        }

        private void CreateHeaderCell(IRow row, int column, string value)
        {
            var cell = row.CreateCell(column);
            var style = cell.Sheet.Workbook.CreateCellStyle();
            var font = cell.Sheet.Workbook.CreateFont();
            font.IsBold = true;
            style.SetFont(font);
            cell.CellStyle = style;
            cell.SetCellValue(value);
        }

        private string? GetCellValue(ICell? cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                default:
                    return null;
            }
        }
    }
}