using Microsoft.CodeAnalysis.CSharp.Syntax;
using SplitFile.Forms;
using SplitFile.Models;
using SplitFile.Services;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using FileToolLib.Services;

namespace SplitFile
{
    public partial class frmSplitFile : Form
    {
        #region "Declare"
        private readonly CodeAnalyzer _codeAnalyzer;
        private readonly List<FileEntry> _files;
        private bool _isAnalyzed;
        private AnalizeResult? _analizeResult;
        #endregion

        #region "Property"
        private List<SplitConfig> SplitConfigs => GetSplitConfigs();
        #endregion

        #region "Constants"
        private const string EXCEL_FILTER = "Excel Files (*.xlsx)|*.xlsx|Excel 97-2003 Files (*.xls)|*.xls|All files (*.*)|*.*";
        private const string IMPORT_TITLE = "Chọn file cấu hình nhập khẩu";
        private const string EXPORT_TITLE = "Chọn nơi lưu file cấu hình";
        private const string SAMPLE_TITLE = "Chọn nơi lưu file mẫu";
        #endregion

        #region "Construct"
        private readonly ExcelService _excelService;
        public frmSplitFile()
        {
            InitializeComponent();
            _codeAnalyzer = new CodeAnalyzer();
            _excelService = new ExcelService();
            _files = new List<FileEntry>();
            dgvSplitConfig.AutoGenerateColumns = false;
            SetupDataGridView();
            this.btnExportExcel.Click += new System.EventHandler(this.btnExportExcel_Click);
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
        }
        #endregion

        #region "Import/Export Methods"
        private void ImportFromExcel(string filePath)
        {
            try
            {
                // Get list of sheets
                var sheets = _excelService.GetSheetNames(filePath);
                if (!sheets.Any())
                {
                    MessageBox.Show("File Excel không có sheet nào!", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Show sheet selector dialog
                using var dialog = new SheetSelectorDialog(sheets);
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                var configs = _excelService.ReadExcel(filePath, dialog.SelectedSheet);
                if (!configs.Any())
                {
                    MessageBox.Show("Không tìm thấy dữ liệu hợp lệ trong file!", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Clear current files before processing new import
                _files.Clear();
                lstFiles.Items.Clear();

                // Add distinct main files from config to the list if they are specified
                var distinctMainFilePathsFromConfig = configs
                    .Where(c => !string.IsNullOrEmpty(c.MainFile))
                    .Select(c => c.MainFile)
                    .Distinct()
                    .ToList();

                foreach (var mainFilePath in distinctMainFilePathsFromConfig)
                {
                    if (!_files.Any(f => f.FilePath == mainFilePath))
                    {
                        var fileEntry = new FileEntry(mainFilePath) { IsMainFile = true }; // Assume it's a main file
                        _files.Add(fileEntry);
                        var item = lstFiles.Items.Add(mainFilePath);
                        item.SubItems.Add("Có"); // Mark as main file
                    }
                }

                // Process file configurations to add original files
                var mainFiles = configs.Where(c => c.IsMainFile).ToList();
                var otherFiles = configs.Where(c => !c.IsMainFile).ToList();

                // Add main files first
                foreach (var config in mainFiles)
                {
                    if (!_files.Any(f => f.FilePath == config.OriginalFile))
                    {
                        var fileEntry = new FileEntry(config.OriginalFile) { IsMainFile = true };
                        _files.Add(fileEntry);
                        var item = lstFiles.Items.Add(config.OriginalFile);
                        item.SubItems.Add("Có");
                    }
                    // If OriginalFile is a main file and also listed as a MainFile in config, ensure it's marked as main
                    var existingEntry = _files.FirstOrDefault(f => f.FilePath == config.OriginalFile);
                    if (existingEntry != null && !existingEntry.IsMainFile)
                    {
                        existingEntry.IsMainFile = true;
                        // Update ListViewItem if necessary - this might be complex if not all items are simple strings
                        // For simplicity, we assume the initial add correctly sets the subitem.
                        // If OriginalFile was added from distinctMainFilePathsFromConfig, its IsMainFile status might need update here.
                        // However, the current logic adds distinctMainFilePathsFromConfig first and marks them "Có".
                        // Then it adds OriginalFiles. If an OriginalFile is a main file, it's also marked "Có".
                        // A file appearing in both distinctMainFilePathsFromConfig and as an OriginalFile (main)
                        // would be added once by the first loop, and the second loop would skip it due to `!_files.Any(...)`.
                        // If an OriginalFile (main) was NOT in distinctMainFilePathsFromConfig, it's added here.
                        // Let's ensure the IsMainFile status for OriginalFile is prioritized.
                        var listItem = lstFiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text == config.OriginalFile);
                        if (listItem != null && listItem.SubItems.Count > 1)
                        {
                            listItem.SubItems[1].Text = "Có";
                        }
                    }
                }

                // Add other files
                foreach (var config in otherFiles)
                {
                    if (!_files.Any(f => f.FilePath == config.OriginalFile))
                    {
                        var fileEntry = new FileEntry(config.OriginalFile) { IsMainFile = false };
                        _files.Add(fileEntry);
                        var item = lstFiles.Items.Add(config.OriginalFile);
                        item.SubItems.Add("");
                    }
                }

                // Clear split configurations
                while (dgvSplitConfig.Rows.Count > 1)
                {
                    dgvSplitConfig.Rows.RemoveAt(0);
                }

                // Add split configurations
                foreach (var config in configs)
                {
                    var rowIndex = dgvSplitConfig.Rows.Add();
                    var row = dgvSplitConfig.Rows[rowIndex];
                    row.Cells["colOriginalFile"].Value = config.OriginalFile;
                    row.Cells["colIsMain"].Value = config.IsMainFile ? "Yes" : "No";
                    row.Cells["colMainFile"].Value = config.MainFile;
                    row.Cells["colFileName"].Value = config.NewFileName;
                    row.Cells["colDescription"].Value = config.Description;
                    row.Cells["colMethods"].Value = config.Methods;

                    // Handle MainFile column state
                    if (config.IsMainFile)
                    {
                        row.Cells["colMainFile"].ReadOnly = true;
                        row.Cells["colMainFile"].Style.BackColor = System.Drawing.SystemColors.Control;
                    }
                }

                MessageBox.Show($"Đã nhập khẩu {configs.Count} cấu hình từ sheet '{dialog.SelectedSheet}' thành công!", 
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhập khẩu file: {ex.Message}", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExportConfig_Click(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = EXCEL_FILTER,
                Title = SAMPLE_TITLE,
                DefaultExt = "xlsx",
                FileName = "SplitFileTemplate.xlsx"
            };

            string lastPath = MemoryBank.LoadPathSetting("SplitFile_ExportSample_LastPath");
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                dialog.InitialDirectory = lastPath;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    MemoryBank.SavePathSetting("SplitFile_ExportSample_LastPath", Path.GetDirectoryName(dialog.FileName));

                    _excelService.CreateSampleFile(dialog.FileName);
                    
                    // Mở file Excel sau khi tạo xong
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);

                    MessageBox.Show("Đã tạo file mẫu thành công và mở file để chỉnh sửa.", 
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tạo file mẫu: {ex.Message}", 
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnImportConfig_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = EXCEL_FILTER,
                Title = IMPORT_TITLE
            };

            string lastPath = MemoryBank.LoadPathSetting("SplitFile_ImportConfig_LastPath");
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                dialog.InitialDirectory = lastPath;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                MemoryBank.SavePathSetting("SplitFile_ImportConfig_LastPath", Path.GetDirectoryName(dialog.FileName));
                ImportFromExcel(dialog.FileName);
            }
        }
        #endregion

        #region "Events"
        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "C# Files|*.cs",
                Title = "Chọn file C# cần tách",
                Multiselect = true
            };

            string lastPath = MemoryBank.LoadPathSetting("SplitFile_AddFiles_LastPath");
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                dialog.InitialDirectory = lastPath;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (dialog.FileNames.Length > 0)
                {
                    MemoryBank.SavePathSetting("SplitFile_AddFiles_LastPath", Path.GetDirectoryName(dialog.FileNames[0]));
                }
                foreach (var filePath in dialog.FileNames)
                {
                    if (!_files.Any(f => f.FilePath == filePath))
                    {
                        var fileEntry = new FileEntry(filePath);
                        // If this is the first file, set it as main
                        if (!_files.Any()) fileEntry.IsMainFile = true;
                        _files.Add(fileEntry);
                        var item = lstFiles.Items.Add(filePath);
                        item.SubItems.Add(fileEntry.IsMainFile ? "Có" : "");
                    }
                }
                _isAnalyzed = false;
                _analizeResult = null;
            }
        }

        private void btnSetMainFile_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count != 1)
            {
                MessageBox.Show("Vui lòng chọn một file để đặt làm file chính!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedFile = lstFiles.SelectedItems[0];
            var filePath = selectedFile.Text;

            // Reset all main files
            foreach (var file in _files)
            {
                file.IsMainFile = false;
            }
            foreach (ListViewItem item in lstFiles.Items)
            {
                item.SubItems[1].Text = "";
            }

            // Set new main file
            var newMainFile = _files.First(f => f.FilePath == filePath);
            newMainFile.IsMainFile = true;
            selectedFile.SubItems[1].Text = "Có";
            _isAnalyzed = false;
            _analizeResult = null;
        }

        private void btnRemoveFiles_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn file để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedPaths = lstFiles.SelectedItems.Cast<ListViewItem>().Select(item => item.Text).ToList();
            foreach (var path in selectedPaths)
            {
                _files.RemoveAll(f => f.FilePath == path);
            }

            foreach (ListViewItem item in lstFiles.SelectedItems)
            {
                lstFiles.Items.Remove(item);
            }
            _isAnalyzed = false;
            _analizeResult = null;
        }

        private async void btnAnalyze_Click(object sender, EventArgs e)
        {
            if (!ValidateFiles())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một file để phân tích!", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Call the new centralized analysis logic
            await RunAnalysisLogic();
        }

        private async void btnRemoveUnused_Click(object sender, EventArgs e)
        {
            if (!_isAnalyzed)
            {
                MessageBox.Show("Vui lòng phân tích file trước khi xóa các thành phần không sử dụng!", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                FileEntry? mainFileEntry = _files.FirstOrDefault(f => f.IsMainFile);
                if (mainFileEntry == null)
                {
                    if (_files.Any())
                    {
                        mainFileEntry = _files.First();
                    }
                    else
                    {
                         MessageBox.Show("Không có file nào để thực hiện thao tác này.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                         return;
                    }
                }
                
                var otherFiles = _files.Where(f => f != mainFileEntry).Select(f => f.FilePath).ToList();

                // Tìm các thành phần không sử dụng
                var unusedMembers = _codeAnalyzer.FindUnusedMembers();
                if (!unusedMembers.Any())
                {
                    MessageBox.Show("Không tìm thấy thành phần nào không được sử dụng!", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Hiển thị dialog xác nhận
                using var dialog = new UnusedMembersDialog(unusedMembers);
                if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedMembers.Any())
                {
                    await _codeAnalyzer.RemoveUnusedMembers(dialog.SelectedMembers);
                    await _codeAnalyzer.AnalyzeFiles(mainFileEntry.FilePath, otherFiles);
                    MessageBox.Show($"Đã xóa {dialog.SelectedMembers.Count} thành phần không sử dụng!", 
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa thành phần không sử dụng: {ex.Message}", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void btnSplit_Click(object sender, EventArgs e)
        {
            // 1. Validate files selected
            if (!ValidateFiles())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một file để thực hiện tách!", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Validate split configurations exist
            var configs = SplitConfigs;
            if (configs.Count == 0)
            {
                MessageBox.Show("Vui lòng cấu hình ít nhất một file tách!", 
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Always perform analysis before splitting to ensure up-to-date content
            bool analysisSuccess = await RunAnalysisLogic();
            if (!analysisSuccess)
            {
                // RunAnalysisLogic shows its own error message
                return; 
            }

            // 4. Proceed with splitting
            // At this point, analysis is guaranteed to be done and successful.
            await SplitFile(configs);
        }

        private async void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (!_isAnalyzed || _analizeResult == null || _codeAnalyzer.Methods == null || !_codeAnalyzer.Methods.Any())
            {
                MessageBox.Show("Chưa có dữ liệu phân tích để xuất. Vui lòng nhấn nút 'Phân tích' trước.",
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileEntry? mainFileEntry = _files.FirstOrDefault(f => f.IsMainFile) ?? _files.FirstOrDefault();

            if (mainFileEntry == null) // Chỉ xảy ra nếu _files rỗng
            {
                MessageBox.Show("Không có file nào được chọn để lấy thông tin xuất Excel.",
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(mainFileEntry.FilePath);
            string defaultInitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OutPut");
            // Đảm bảo thư mục OutPut tồn tại để SaveFileDialog có thể mở ở đó ban đầu
            if (!Directory.Exists(defaultInitialDirectory))
            {
                Directory.CreateDirectory(defaultInitialDirectory);
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = EXCEL_FILTER; // Sử dụng hằng số đã định nghĩa
                saveFileDialog.Title = "Chọn nơi lưu file kết quả phân tích";
                saveFileDialog.DefaultExt = "xlsx";
                saveFileDialog.FileName = $"{fileNameWithoutExtension}_AnalysisResult.xlsx";

                string lastPath = MemoryBank.LoadPathSetting("SplitFile_ExportAnalysis_LastPath");
                if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                {
                    saveFileDialog.InitialDirectory = lastPath;
                }
                else
                {
                    saveFileDialog.InitialDirectory = defaultInitialDirectory; // Fallback to default logic
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    MemoryBank.SavePathSetting("SplitFile_ExportAnalysis_LastPath", Path.GetDirectoryName(saveFileDialog.FileName));

                    string excelFilePath = saveFileDialog.FileName;
                    try
                    {
                        if (_codeAnalyzer.ClassNodes == null) 
                        {
                             MessageBox.Show("Không thể truy cập thông tin class từ bộ phân tích.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                             return;
                        }
        
                        // Sắp xếp methods cho việc xuất Excel:
                        // 1. Nhóm theo file
                        // 2. Sắp xếp các nhóm file theo tổng độ dài method giảm dần
                        // 3. Sắp xếp methods trong mỗi file theo độ dài giảm dần
                        // 4. Trải phẳng lại thành danh sách methods
                        var sortedMethodsForExcel = _codeAnalyzer.Methods
                            .GroupBy(m => m.SourceFileName)
                            .OrderByDescending(g => g.Sum(m => m.Length))
                            .SelectMany(g => g.OrderByDescending(m => m.Length))
                            .ToList();
        
                        _excelService.ExportAnalysisResult(_analizeResult, sortedMethodsForExcel, excelFilePath, mainFileEntry.FilePath, _codeAnalyzer.ClassNodes);
        
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = excelFilePath,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
        
                        MessageBox.Show($"Đã xuất dữ liệu phân tích ra file: {excelFilePath}\nFile sẽ được tự động mở.",
                                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}",
                                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtResult.Clear();
        }
        #endregion

        #region "Sub/Function"
        private void SetupDataGridView()
        {
            // Setup IsMain combobox
            var isMainColumn = dgvSplitConfig.Columns["colIsMain"] as DataGridViewComboBoxColumn;
            if (isMainColumn != null)
            {
                isMainColumn.Items.AddRange(new[] { "Yes", "No" });
                isMainColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            // Setup columns behavior
            var methodsColumn = dgvSplitConfig.Columns["colMethods"] as DataGridViewTextBoxColumn;
            if (methodsColumn != null)
            {
                methodsColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }

            // Auto adjust row height for wrapped text
            dgvSplitConfig.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Add event handler for IsMain changed
            dgvSplitConfig.CellValueChanged += dgvSplitConfig_CellValueChanged;

            // Add initial row
            dgvSplitConfig.Rows.Add();
        }

        private void dgvSplitConfig_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Handle IsMain column changes
            if (dgvSplitConfig.Columns[e.ColumnIndex].Name == "colIsMain")
            {
                var row = dgvSplitConfig.Rows[e.RowIndex];
                var isMainValue = row.Cells["colIsMain"].Value?.ToString();

                // Enable/disable MainFile based on IsMain value
                if (isMainValue == "Yes")
                {
                    row.Cells["colMainFile"].ReadOnly = true;
                    row.Cells["colMainFile"].Value = null;
                    row.Cells["colMainFile"].Style.BackColor = System.Drawing.SystemColors.Control;
                }
                else if (isMainValue == "No")
                {
                    row.Cells["colMainFile"].ReadOnly = false;
                    row.Cells["colMainFile"].Style.BackColor = System.Drawing.SystemColors.Window;
                }
            }
        }

        private List<SplitConfig> GetSplitConfigs()
        {
            var configs = new List<SplitConfig>();

            foreach (DataGridViewRow row in dgvSplitConfig.Rows)
            {
                if (row.IsNewRow) continue;

                var originalFile = row.Cells["colOriginalFile"].Value?.ToString();
                var isMain = row.Cells["colIsMain"].Value?.ToString() == "Yes";
                var mainFile = row.Cells["colMainFile"].Value?.ToString();
                var fileName = row.Cells["colFileName"].Value?.ToString();
                var methods = row.Cells["colMethods"].Value?.ToString();
                var description = row.Cells["colDescription"].Value?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(originalFile) || string.IsNullOrWhiteSpace(fileName) || 
                    string.IsNullOrWhiteSpace(methods))
                    continue;

                if (!isMain && string.IsNullOrWhiteSpace(mainFile))
                    continue;

                var methodNames = Regex.Split(methods, @"[\r\n\s,]+")
                     .Select(m =>
                     {
                         var parenIndex = m.IndexOf('(');
                         var name = parenIndex > 0 ? m.Substring(0, parenIndex) : m;
                         return name.Trim();
                     })
                     .Where(m => !string.IsNullOrWhiteSpace(m))
                     .Distinct()
                     .ToList();

                var config = new SplitConfig
                {
                    OriginalFile = originalFile,
                    IsMainFile = isMain,
                    MainFile = mainFile,
                    NewFileName = fileName,
                    MethodNames = methodNames,
                    Description = description
                };
                configs.Add(config);
            }

            return configs;
        }

        private async Task<bool> RunAnalysisLogic()
        {
            try
            {
                FileEntry? mainFileEntry = _files.FirstOrDefault(f => f.IsMainFile);
                if (mainFileEntry == null)
                {
                    if (_files.Any())
                    {
                        mainFileEntry = _files.First();
                        // Không tự động set IsMainFile = true ở đây để tránh side effect không mong muốn lên UI
                    }
                    else
                    {
                        // Điều này không nên xảy ra nếu ValidateFiles() hoạt động đúng trước khi gọi RunAnalysisLogic
                        txtResult.Text = "Lỗi: Không có file nào được chọn để phân tích.";
                        return false;
                    }
                }

                var otherFiles = _files.Where(f => f != mainFileEntry).Select(f => f.FilePath).ToList();

                // Calculate total character count
                var mainContent = await File.ReadAllTextAsync(mainFileEntry.FilePath);
                int totalChars = mainContent.Length;

                foreach (var file in otherFiles)
                {
                    var content = await File.ReadAllTextAsync(file);
                    totalChars += content.Length;
                }

                _analizeResult = await _codeAnalyzer.AnalyzeFiles(mainFileEntry.FilePath, otherFiles);
                _isAnalyzed = true;
                txtResult.Clear();

                // Display analysis results
                var allMethods = _codeAnalyzer.Methods;

                // Sắp xếp tất cả các methods theo Length giảm dần (lớn đến nhỏ)
                // var sortedMethods = allMethods.OrderByDescending(m => m.Length).ToList();

                // Nhóm các methods theo SourceFileName, sau đó sắp xếp các group theo tổng kích thước methods giảm dần
                var groupedByFile = allMethods
                    .GroupBy(m => m.SourceFileName) 
                    .OrderByDescending(g => g.Sum(m => m.Length)); // Sắp xếp các file theo tổng kích thước methods giảm dần

                var resultLines = new List<string>
                {
                    $"// Tổng số ký tự của các file đã chọn: {totalChars:N0}",
                    string.Empty
                    // $"// Danh sách các phương thức (sắp xếp theo kích thước giảm dần):"
                };

                // if (sortedMethods.Any())
                // {
                //     foreach (var method in sortedMethods)
                //     {
                //         resultLines.Add($"{method.Name} ({method.Length} ký tự) - File: {Path.GetFileName(method.SourceFile)}");
                //     }
                // }
                if (groupedByFile.Any())
                {
                    resultLines.Add($"// Chi tiết các phương thức theo file (sắp xếp theo kích thước giảm dần trong mỗi file):");
                    resultLines.Add(string.Empty);

                    foreach (var fileGroup in groupedByFile)
                    {
                        long totalCharsInFile = fileGroup.Sum(m => m.Length);
                        resultLines.Add($"// File: {fileGroup.Key} (Tổng ký tự methods: {totalCharsInFile:N0})");
                        // Sắp xếp các methods trong mỗi file theo Length giảm dần
                        var sortedMethodsInFile = fileGroup.OrderByDescending(m => m.Length).ToList();
                        
                        if (sortedMethodsInFile.Any())
                        {
                            foreach (var method in sortedMethodsInFile)
                            {
                                resultLines.Add($"  {method.Name} ({method.Length} ký tự)");
                            }
                        }
                        else
                        {
                            resultLines.Add("  (Không có phương thức nào được tìm thấy trong file này)");
                        }
                        resultLines.Add(string.Empty); // Dòng trống sau mỗi file cho dễ đọc
                    }
                }
                else
                {
                    resultLines.Add("// Không tìm thấy phương thức nào trong các file đã chọn.");
                }
                
                // resultLines.Add(string.Empty); 

                txtResult.Lines = resultLines.ToArray();
                return true; // Analysis successful
            }
            catch (Exception ex)
            {
                _isAnalyzed = false;
                _analizeResult = null;
                txtResult.Clear(); // Xóa kết quả cũ nếu có lỗi
                // Hiển thị lỗi trong txtResult, có thể dùng AppendText nếu muốn giữ lại thông báo trước đó (nếu có)
                txtResult.Text = $"Lỗi khi phân tích file: {ex.Message}\r\n\r\nChi tiết lỗi:\r\n{ex.StackTrace}"; 
                // MessageBox.Show($"Lỗi khi phân tích file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // Analysis failed
            }
        }

        private bool ValidateFiles()
        {
            if (_files.Count == 0) return false;
            // if (!_files.Any(f => f.IsMainFile)) return false;
            return true;
        }

        private async Task SplitFile(List<SplitConfig> configs)
        {
            try
            {
                // Group configs by source file to process each file only once
                var configsBySource = new Dictionary<string, List<(string targetFile, List<string> methods, string description)>>();
                
                foreach (var config in configs)
                {
                    var sourceFile = config.OriginalFile;// config.IsMainFile ? config.OriginalFile : config.MainFile;
                    var targetFile = GetNewFilePath(config);

                    if (!configsBySource.ContainsKey(sourceFile))
                    {
                        configsBySource[sourceFile] = new List<(string, List<string>, string)>();
                    }

                    configsBySource[sourceFile].Add((targetFile, config.MethodNames, config.Description));
                }

                // Process each source file once
                foreach (var sourceConfig in configsBySource)
                {
                    await _codeAnalyzer.CreateNewFiles(sourceConfig.Key, sourceConfig.Value);
                }

                MessageBox.Show("Đã tách methods thành công và cập nhật các file gốc",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tách file: {ex.Message}",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetNewFilePath(SplitConfig config)
        {
            var sourceFile = config.IsMainFile ? config.OriginalFile : config.MainFile;
            var fileDirectory = Path.GetDirectoryName(sourceFile);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFile);
            var extension = Path.GetExtension(sourceFile);

            return Path.Combine(fileDirectory!, $"{fileNameWithoutExt}.{config.NewFileName}{extension}");
        }
        #endregion
    }
}