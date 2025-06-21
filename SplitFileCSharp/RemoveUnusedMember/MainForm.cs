using Microsoft.CodeAnalysis;
using RemoveUnusedMember.Properties;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text; // For StringBuilder in WriteLastCsvPathToMemoryBank
using System.Data; // For DataTable if used for ComboBox data source
using OfficeOpenXml; // Added for EPPlus
using OfficeOpenXml.Style; // Optional: For styling

namespace RemoveUnusedMember
{
    public partial class MainForm : Form
    {
        private CodeAnalyzer _analyzer;
        private CodeRemover _remover;
        private List<UnusedMemberInfo> _currentUnusedMembers = new List<UnusedMemberInfo>();
        private AdhocWorkspace _currentWorkspace;
        private const string UserPreferencesFileName = "userpreferences.md";
        private const string MemoryBankDir = "memory-bank";
        private const string LastExcelPathKey = "LastExportImportPathExcel:";

        public MainForm()
        {
            InitializeComponent();
            LoadLastUsedFolderPath();
            this.btnSelectFolder.Click += BtnSelectFolder_Click;
            this.btnScan.Click += BtnScan_Click;
            this.btnRefresh.Click += BtnRefresh_Click;
            this.btnDeleteSelected.Click += BtnDeleteSelected_Click;
            this.btnCopySelected.Click += BtnCopySelected_Click;
            this.btnFilter.Click += BtnFilter_Click;
            this.txtFilter.KeyDown += TxtFilter_KeyDown;
            this.dgvUnusedMembers.CellValueChanged += DgvUnusedMembers_CellValueChanged;
            this.dgvUnusedMembers.CurrentCellDirtyStateChanged += DgvUnusedMembers_CurrentCellDirtyStateChanged;
            this.cmbFilterType.SelectedIndexChanged += FilterControls_Changed;
            this.cmbFilterAccessibility.SelectedIndexChanged += FilterControls_Changed;
            this.cmbFilterFile.SelectedIndexChanged += FilterControls_Changed;
            this.cmbFilterClass.SelectedIndexChanged += FilterControls_Changed;
            this.chkOnlyMethods.CheckedChanged += FilterControls_Changed;
            this.btnExportExcel.Click += BtnExportExcel_Click;
            this.btnImportExcel.Click += BtnImportExcel_Click;
            _analyzer = new CodeAnalyzer();
            _remover = new CodeRemover();
            _currentWorkspace = new AdhocWorkspace();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void LoadLastUsedFolderPath()
        {
            txtFolderPath.Text = Settings.Default.LastUsedFolderPath;
        }

        private void SaveLastUsedFolderPath()
        {
            Settings.Default.LastUsedFolderPath = txtFolderPath.Text;
            Settings.Default.Save();
        }

        private void BtnSelectFolder_Click(object? sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text = folderBrowserDialog.SelectedPath;
                SaveLastUsedFolderPath();
            }
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void BtnScan_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("Vui lòng chọn một đường dẫn thư mục hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtResults.Clear();
            dgvUnusedMembers.Rows.Clear();
            _currentUnusedMembers.Clear();
            SetControlsEnabled(false);
            Log("Bắt đầu quét thư mục: " + txtFolderPath.Text
                + (chkOnlyMethods.Checked ? " (chỉ phương thức)" : " (tất cả thành phần)"));

            _currentWorkspace = new AdhocWorkspace();

            var progress = new Progress<string>(Log);

            try
            {
                _currentUnusedMembers = await _analyzer.AnalyzeProjectAsync(txtFolderPath.Text, _currentWorkspace, progress, chkOnlyMethods.Checked);

                if (_currentUnusedMembers.Any())
                {
                    Log($"Tìm thấy {_currentUnusedMembers.Count} thành phần có thể không sử dụng.");
                    PopulateFilterComboBoxes();
                    ApplyCombinedFilters();
                }
                else
                {
                    Log("Không tìm thấy thành phần nào không sử dụng (dựa trên phân tích hiện tại).");
                    dgvUnusedMembers.Rows.Clear();
                    PopulateFilterComboBoxes();
                }

                Log("Hoàn thành quét.");
            }
            catch (Exception ex)
            {
                Log("Lỗi trong quá trình quét: " + ex.Message);
                MessageBox.Show("Đã xảy ra lỗi trong quá trình quét: \n" + ex.ToString(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtFolderPath.Enabled = enabled;
            btnSelectFolder.Enabled = enabled;
            btnScan.Enabled = enabled;
            btnDeleteSelected.Enabled = enabled;
            btnRefresh.Enabled = enabled;
            txtFilter.Enabled = enabled;
            btnFilter.Enabled = enabled;
            cmbFilterType.Enabled = enabled && _currentUnusedMembers.Any();
            cmbFilterAccessibility.Enabled = enabled && _currentUnusedMembers.Any();
            cmbFilterFile.Enabled = enabled && _currentUnusedMembers.Any();
            cmbFilterClass.Enabled = enabled && _currentUnusedMembers.Any();
            chkOnlyMethods.Enabled = enabled;
        }

        private void Log(string message)
        {
            if (txtResults.InvokeRequired)
            {
                txtResults.Invoke(new Action(() => Log(message)));
            }
            else
            {
                if (txtResults != null)
                {
                    txtResults.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
        }

        private async void BtnDeleteSelected_Click(object? sender, EventArgs e)
        {
            var membersToProcessForDeletion = new List<UnusedMemberInfo>();
            var rowsToRemoveFromGrid = new List<DataGridViewRow>();

            foreach (DataGridViewRow row in dgvUnusedMembers.Rows)
            {
                if (row.IsNewRow) continue;

                bool isChecked = false;
                if (row.Cells["colSelect"].Value != null && row.Cells["colSelect"].Value != DBNull.Value)
                {
                    isChecked = Convert.ToBoolean(row.Cells["colSelect"].Value);
                }

                if (isChecked)
                {
                    // Lấy thông tin định danh từ dòng để tìm UnusedMemberInfo tương ứng
                    string? filePath = row.Cells["colFilePath"]?.Value?.ToString();
                    string? memberName = row.Cells["colName"]?.Value?.ToString();
                    int lineNumber = 0;
                    if (row.Cells["colLineNumber"]?.Value != null && row.Cells["colLineNumber"].Value != DBNull.Value)
                    {
                        int.TryParse(row.Cells["colLineNumber"].Value.ToString(), out lineNumber);
                    }
                    string? accessibility = row.Cells["colAccessibility"]?.Value?.ToString();
                    string? typeStr = row.Cells["colType"]?.Value?.ToString();
                    MemberType memberType = MemberType.Other;
                    if (!string.IsNullOrEmpty(typeStr) && Enum.TryParse(typeStr, out MemberType parsedType))
                    {
                        memberType = parsedType;
                    }

                    if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(memberName) || lineNumber == 0)
                    {
                        Log($"CẢNH BÁO: Dòng {row.Index} được chọn nhưng thiếu thông tin để xác định thành phần. Bỏ qua dòng này.");
                        continue;
                    }

                    var memberInfo = _currentUnusedMembers.FirstOrDefault(m =>
                                        m.FilePath == filePath &&
                                        m.Name == memberName &&
                                        m.LineNumber == lineNumber &&
                                        m.Type == memberType &&
                                        m.Accessibility == accessibility);

                    if (memberInfo != null)
                    {
                        membersToProcessForDeletion.Add(memberInfo);
                        rowsToRemoveFromGrid.Add(row); // Lưu lại dòng để xóa khỏi grid sau
                    }
                    else
                    {
                        Log($"CẢNH BÁO: Không tìm thấy UnusedMemberInfo tương ứng trong _currentUnusedMembers cho dòng được chọn: {memberName} tại {filePath} dòng {lineNumber}. Dòng này sẽ không được xóa.");
                    }
                }
            }

            if (!membersToProcessForDeletion.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một thành phần để xóa.", "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show($"Bạn có chắc chắn muốn xóa {membersToProcessForDeletion.Count} thành phần đã chọn không? Hành động này không thể hoàn tác và có thể thay đổi mã nguồn của bạn.",
                                                "Xác nhận xóa",
                                                MessageBoxButtons.YesNo,
                                                MessageBoxIcon.Warning);

            if (confirmResult == DialogResult.Yes)
            {
                Log($"Bắt đầu xóa {membersToProcessForDeletion.Count} thành phần...");
                SetControlsEnabled(false);
                int successFilesCount = 0;
                int failedFilesCount = 0;

                try
                {
                    var changesByFile = membersToProcessForDeletion.GroupBy(m => m.FilePath);

                    foreach (var group in changesByFile)
                    {
                        string filePath = group.Key;
                        var membersInFile = group.ToList();

                        bool success = await _remover.RemoveMembersAsync(filePath, membersInFile, _currentWorkspace);
                        foreach (var msg in _remover.LogMessages) // Log từ _remover nên được ghi
                        {
                            Log("  [Remover] " + msg); // Phân biệt log từ remover
                        }

                        if (success)
                        {
                            Log($"Đã xử lý xong tệp: {filePath} (thành công hoặc có cảnh báo đã được log bởi Remover)");
                            successFilesCount++;
                        }
                        else
                        {
                            Log($"Xử lý tệp {filePath} thất bại hoàn toàn (xem log của Remover).");
                            failedFilesCount++;
                        }
                    }

                    Log($"Hoàn thành xóa. {successFilesCount} tệp được xử lý, {failedFilesCount} tệp gặp lỗi nghiêm trọng khi xử lý.");

                    // Cập nhật lại DataGridView và danh sách nội bộ _currentUnusedMembers
                    // Xóa các dòng đã được xử lý khỏi grid
                    foreach (var row in rowsToRemoveFromGrid.OrderByDescending(r => r.Index)) // Xóa từ dưới lên để tránh lỗi index
                    {
                        if (!row.IsNewRow) dgvUnusedMembers.Rows.Remove(row);
                    }
                    // Xóa các thành viên đã xử lý khỏi danh sách nguồn
                    _currentUnusedMembers.RemoveAll(m => membersToProcessForDeletion.Contains(m));

                }
                catch (Exception ex)
                {
                    Log("Lỗi nghiêm trọng trong quá trình xóa: " + ex.Message + ex.StackTrace);
                    MessageBox.Show("Đã xảy ra lỗi nghiêm trọng trong quá trình xóa: \n" + ex.ToString(), "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetControlsEnabled(true);
                    Log("Nên thực hiện Quét (Scan) lại để cập nhật trạng thái sau khi xóa.");
                }
            }
            else
            {
                Log("Hủy bỏ thao tác xóa.");
            }
        }

        private void BtnCopySelected_Click(object? sender, EventArgs e)
        {
            var selectedData = new System.Text.StringBuilder();
            selectedData.AppendLine("Selected\tType\tName\tFilePath\tLineNumber\tContainingType"); // Header

            // Lấy dữ liệu từ các dòng đang hiển thị trên grid (có thể đã được lọc)
            foreach (DataGridViewRow row in dgvUnusedMembers.Rows)
            {
                if (row.Cells["colSelect"].Value != null && Convert.ToBoolean(row.Cells["colSelect"].Value))
                {
                    selectedData.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}{6}",
                        true, // Vì đã được chọn
                        row.Cells["colType"].Value?.ToString() ?? string.Empty,
                        row.Cells["colName"].Value?.ToString() ?? string.Empty,
                        row.Cells["colFilePath"].Value?.ToString() ?? string.Empty,
                        row.Cells["colLineNumber"].Value?.ToString() ?? string.Empty,
                        row.Cells["colContainingType"].Value?.ToString() ?? string.Empty,
                        Environment.NewLine);
                }
            }

            if (selectedData.ToString().Split(Environment.NewLine).Length > 2) // Header + ít nhất 1 dòng dữ liệu + dòng trống cuối
            {
                Clipboard.SetText(selectedData.ToString());
                Log($"Đã sao chép {selectedData.ToString().Split(Environment.NewLine).Length - 2} mục vào clipboard.");
            }
            else
            {
                Log("Không có mục nào được chọn để sao chép.");
            }
        }

        private void TxtFilter_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyCombinedFilters();
                e.SuppressKeyPress = true;
            }
        }

        private void BtnFilter_Click(object? sender, EventArgs e)
        {
            ApplyCombinedFilters();
        }

        private void FilterControls_Changed(object? sender, EventArgs e)
        {
            ApplyCombinedFilters();
        }

        private void PopulateFilterComboBoxes()
        {
            Action<ComboBox, IEnumerable<string?>> populateCombo = (combo, data) =>
            {
                string? previousValue = combo.SelectedItem?.ToString();
                combo.Items.Clear();
                combo.Items.Add("Tất cả");
                var distinctValues = data.Where(d => !string.IsNullOrEmpty(d)).Distinct().OrderBy(d => d).ToArray();
                if (distinctValues.Any())
                {
                    combo.Items.AddRange(distinctValues);
                }

                if (!string.IsNullOrEmpty(previousValue) && combo.Items.Contains(previousValue))
                {
                    combo.SelectedItem = previousValue;
                }
                else
                {
                    combo.SelectedIndex = 0;
                }
            };

            populateCombo(cmbFilterType, _currentUnusedMembers.Select(m => m.Type.ToString()));
            populateCombo(cmbFilterAccessibility, _currentUnusedMembers.Select(m => m.Accessibility?.Split('(')[0].Trim()));
            populateCombo(cmbFilterFile, _currentUnusedMembers.Select(m => m.FilePath));
            populateCombo(cmbFilterClass, _currentUnusedMembers.Select(m => m.ContainingType));

            cmbFilterType.Enabled = cmbFilterType.Items.Count > 1;
            cmbFilterAccessibility.Enabled = cmbFilterAccessibility.Items.Count > 1;
            cmbFilterFile.Enabled = cmbFilterFile.Items.Count > 1;
            cmbFilterClass.Enabled = cmbFilterClass.Items.Count > 1;
        }

        private void ApplyCombinedFilters()
        {
            string filterText = txtFilter.Text.Trim().ToLowerInvariant();
            string? selectedType = cmbFilterType.SelectedItem?.ToString();
            string? selectedAccessibility = cmbFilterAccessibility.SelectedItem?.ToString();
            string? selectedFile = cmbFilterFile.SelectedItem?.ToString();
            string? selectedClass = cmbFilterClass.SelectedItem?.ToString();
            bool onlyMethods = chkOnlyMethods.Checked;

            IEnumerable<UnusedMemberInfo> filteredMembers = _currentUnusedMembers;

            if (onlyMethods)
            {
                filteredMembers = filteredMembers.Where(m => m.Type == MemberType.Method);
            }

            if (selectedType != "Tất cả" && !string.IsNullOrEmpty(selectedType))
            {
                filteredMembers = filteredMembers.Where(m => m.Type.ToString() == selectedType);
            }

            if (selectedAccessibility != "Tất cả" && !string.IsNullOrEmpty(selectedAccessibility))
            {
                filteredMembers = filteredMembers.Where(m => m.Accessibility?.StartsWith(selectedAccessibility, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            if (selectedFile != "Tất cả" && !string.IsNullOrEmpty(selectedFile))
            {
                filteredMembers = filteredMembers.Where(m => m.FilePath == selectedFile);
            }

            if (selectedClass != "Tất cả" && !string.IsNullOrEmpty(selectedClass))
            {
                filteredMembers = filteredMembers.Where(m => m.ContainingType == selectedClass);
            }

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                filteredMembers = filteredMembers.Where(m =>
                    (m.Name?.ToLowerInvariant().Contains(filterText) ?? false) ||
                    (m.FilePath?.ToLowerInvariant().Contains(filterText) ?? false) ||
                    (m.Accessibility?.ToLowerInvariant().Contains(filterText) ?? false) ||
                    (m.ContainingType?.ToLowerInvariant().Contains(filterText) ?? false)
                );
            }

            dgvUnusedMembers.Rows.Clear();
            var listToDisplay = filteredMembers.ToList();
            if (listToDisplay.Any())
            {
                foreach (var member in listToDisplay)
                {
                    dgvUnusedMembers.Rows.Add(member.IsSelected, member.Type.ToString(), member.Accessibility, member.Name, member.FilePath, member.LineNumber, member.ContainingType);
                }
            }
            Log($"Hiển thị {listToDisplay.Count} mục sau khi áp dụng bộ lọc.");
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            Log("Làm mới kết quả...");
            if (!string.IsNullOrWhiteSpace(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
            {
                BtnScan_Click(sender, e);
            }
            else
            {
                txtResults.Clear();
                dgvUnusedMembers.Rows.Clear();
                _currentUnusedMembers.Clear();
                Log("Vui lòng chọn đường dẫn thư mục hợp lệ trước khi làm mới.");
            }
        }

        private void DgvUnusedMembers_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvUnusedMembers.IsCurrentCellDirty && dgvUnusedMembers.CurrentCell.OwningColumn.Name == "colSelect")
            {
                dgvUnusedMembers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvUnusedMembers_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvUnusedMembers.Columns["colSelect"].Index)
            {
                DataGridViewRow row = dgvUnusedMembers.Rows[e.RowIndex];
                bool isChecked = false;
                if (row.Cells["colSelect"].Value != null && row.Cells["colSelect"].Value != DBNull.Value)
                {
                    isChecked = Convert.ToBoolean(row.Cells["colSelect"].Value);
                }

                // Lấy thông tin định danh duy nhất từ dòng để tìm đối tượng UnusedMemberInfo tương ứng
                // Sử dụng các cột có khả năng định danh cao và ít thay đổi
                string? filePath = row.Cells["colFilePath"]?.Value?.ToString();
                string? memberName = row.Cells["colName"]?.Value?.ToString();
                int lineNumber = 0;
                if (row.Cells["colLineNumber"]?.Value != null && row.Cells["colLineNumber"].Value != DBNull.Value)
                {
                    int.TryParse(row.Cells["colLineNumber"].Value.ToString(), out lineNumber);
                }
                string? accessibility = row.Cells["colAccessibility"]?.Value?.ToString(); // Giả sử cột này tồn tại
                string? typeStr = row.Cells["colType"]?.Value?.ToString(); // Giả sử cột này tồn tại
                MemberType memberType = MemberType.Other;
                if (!string.IsNullOrEmpty(typeStr))
                {
                    Enum.TryParse(typeStr, out memberType);
                }


                if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(memberName) || lineNumber == 0)
                {
                    // Không đủ thông tin để xác định member, có thể là dòng mới hoặc lỗi dữ liệu
                    // Log($"Không đủ thông tin trên dòng {e.RowIndex} để cập nhật trạng thái chọn.");
                    return;
                }

                // Tìm memberInfo trong _currentUnusedMembers dựa trên các thông tin định danh
                var memberInfo = _currentUnusedMembers.FirstOrDefault(m =>
                                    m.FilePath == filePath &&
                                    m.Name == memberName &&
                                    m.LineNumber == lineNumber &&
                                    m.Type == memberType && // Thêm Type và Accessibility để tăng độ chính xác
                                    m.Accessibility == accessibility);

                if (memberInfo != null)
                {
                    if (memberInfo.IsSelected != isChecked)
                    {
                        memberInfo.IsSelected = isChecked;
                        Log($"Đã thay đổi trạng thái chọn của: {memberInfo.Name} ({memberInfo.FilePath} dòng {memberInfo.LineNumber}) thành {memberInfo.IsSelected}");
                    }
                }
                else
                {
                    Log($"Không tìm thấy UnusedMemberInfo tương ứng cho dòng: {memberName} tại {filePath} dòng {lineNumber}");
                }
            }
        }

        private string GetMemoryBankFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MemoryBankDir, fileName);
        }

        private string? ReadLastExcelPathFromMemoryBank()
        {
            string filePath = GetMemoryBankFilePath(UserPreferencesFileName);
            try
            {
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith(LastExcelPathKey))
                        {
                            return line.Substring(LastExcelPathKey.Length).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi khi đọc đường dẫn Excel từ Memory Bank ({filePath}): {ex.Message}");
            }
            return null; // Trả về null nếu không tìm thấy hoặc có lỗi
        }

        private void WriteLastExcelPathToMemoryBank(string newPath)
        {
            string filePath = GetMemoryBankFilePath(UserPreferencesFileName);
            try
            {
                string memoryBankFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, MemoryBankDir);
                if (!Directory.Exists(memoryBankFullPath))
                {
                    Directory.CreateDirectory(memoryBankFullPath);
                }

                var lines = new List<string>();
                bool pathUpdated = false;

                if (File.Exists(filePath))
                {
                    lines.AddRange(File.ReadAllLines(filePath));
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith(LastExcelPathKey))
                        {
                            lines[i] = LastExcelPathKey + " " + newPath;
                            pathUpdated = true;
                            break;
                        }
                        if (lines[i].StartsWith("LastExportImportPathCsv:"))
                        {
                            lines.RemoveAt(i);
                            i--;
                        }
                    }
                }

                if (!pathUpdated)
                {
                    if (!lines.Any(l => l.TrimStart().StartsWith("# User Preferences")))
                    {
                        lines.Insert(0, "# User Preferences");
                    }
                    lines.Add(LastExcelPathKey + " " + newPath);
                }
                File.WriteAllLines(filePath, lines, Encoding.UTF8);
                Log($"Đã lưu đường dẫn Excel vào Memory Bank: {newPath}");
            }
            catch (Exception ex)
            {
                Log($"Lỗi khi ghi đường dẫn Excel vào Memory Bank ({filePath}): {ex.Message}");
            }
        }

        private async void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            if (!_currentUnusedMembers.Any())
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                sfd.Title = "Xuất danh sách thành phần không sử dụng ra Excel";

                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                sfd.InitialDirectory = ReadLastExcelPathFromMemoryBank() ?? outputDir;

                string baseFolderName = string.Empty;
                if (!string.IsNullOrEmpty(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
                {
                    baseFolderName = new DirectoryInfo(txtFolderPath.Text).Name;
                }
                sfd.FileName = !string.IsNullOrEmpty(baseFolderName) ? $"UnusedMember_{baseFolderName}.xlsx" : "UnusedMember_Export.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    WriteLastExcelPathToMemoryBank(Path.GetDirectoryName(sfd.FileName));
                    try
                    {
                        SetControlsEnabled(false);
                        Log("Bắt đầu xuất ra Excel: " + sfd.FileName);

                        using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                        {
                            var worksheet = package.Workbook.Worksheets.Add("UnusedMembers");
                            string[] headers = { "IsSelected", "Type", "Accessibility", "Name", "FilePath", "LineNumber", "ContainingType" };
                            for (int i = 0; i < headers.Length; i++)
                            {
                                worksheet.Cells[1, i + 1].Value = headers[i];
                                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                            }

                            for (int i = 0; i < _currentUnusedMembers.Count; i++)
                            {
                                var member = _currentUnusedMembers[i];
                                worksheet.Cells[i + 2, 1].Value = member.IsSelected;
                                worksheet.Cells[i + 2, 2].Value = member.Type.ToString();
                                worksheet.Cells[i + 2, 3].Value = member.Accessibility;
                                worksheet.Cells[i + 2, 4].Value = member.Name;
                                worksheet.Cells[i + 2, 5].Value = member.FilePath;
                                worksheet.Cells[i + 2, 6].Value = member.LineNumber;
                                worksheet.Cells[i + 2, 7].Value = member.ContainingType;
                            }
                            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                            await package.SaveAsync();
                        }
                        Log($"Đã xuất thành công {_currentUnusedMembers.Count} mục ra tệp Excel: {sfd.FileName}");
                    }
                    catch (Exception ex)
                    {
                        Log("Lỗi khi xuất Excel: " + ex.Message);
                        MessageBox.Show("Lỗi khi xuất Excel: \n" + ex.ToString(), "Lỗi Xuất Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        SetControlsEnabled(true);
                    }
                }
            }
        }

        private async void BtnImportExcel_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                ofd.Title = "Nhập danh sách thành phần từ Excel";
                ofd.InitialDirectory = ReadLastExcelPathFromMemoryBank() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    WriteLastExcelPathToMemoryBank(Path.GetDirectoryName(ofd.FileName));
                    try
                    {
                        SetControlsEnabled(false);
                        Log("Bắt đầu nhập từ Excel: " + ofd.FileName);

                        int updatedCount = 0;
                        using (var package = new ExcelPackage(new FileInfo(ofd.FileName)))
                        {
                            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null)
                            {
                                Log("Tệp Excel không có worksheet nào.");
                                MessageBox.Show("Tệp Excel không có worksheet nào.", "Lỗi Nhập Excel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                SetControlsEnabled(true);
                                return;
                            }

                            int rowCount = worksheet.Dimension?.Rows ?? 0;
                            if (rowCount <= 1)
                            {
                                Log("Tệp Excel không có dữ liệu hoặc chỉ có dòng tiêu đề.");
                                MessageBox.Show("Tệp Excel không có dữ liệu hợp lệ.", "Lỗi Nhập Excel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                SetControlsEnabled(true);
                                return;
                            }

                            for (int row = 2; row <= rowCount; row++)
                            {
                                try
                                {
                                    Func<int, string> getString = (col) => worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? string.Empty;

                                    bool isSelected = bool.TryParse(getString(1), out bool sel) ? sel : false;
                                    MemberType type = Enum.TryParse(getString(2), out MemberType t) ? t : MemberType.Other;
                                    string accessibility = getString(3);
                                    string name = getString(4);
                                    string filePath = getString(5);
                                    int lineNumber = int.TryParse(getString(6), out int ln) ? ln : 0;
                                    string containingType = getString(7);

                                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(filePath) || lineNumber == 0)
                                    {
                                        Log($"Dòng {row} trong Excel thiếu thông tin Name, FilePath hoặc LineNumber. Bỏ qua.");
                                        continue;
                                    }

                                    var importedMember = new UnusedMemberInfo(type, name, filePath, lineNumber, containingType, accessibility) { IsSelected = isSelected };
                                    var existingMember = _currentUnusedMembers.FirstOrDefault(m =>
                                        m.FilePath == importedMember.FilePath &&
                                        m.Name == importedMember.Name &&
                                        m.LineNumber == importedMember.LineNumber &&
                                        m.Type == importedMember.Type &&
                                        m.Accessibility == importedMember.Accessibility);

                                    if (existingMember != null)
                                    {
                                        if (existingMember.IsSelected != importedMember.IsSelected)
                                        {
                                            existingMember.IsSelected = importedMember.IsSelected;
                                            updatedCount++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log($"Lỗi khi phân tích dòng Excel {row}: Lỗi: {ex.Message}");
                                }
                            }
                        }
                        Log($"Hoàn thành nhập. {updatedCount} mục đã được cập nhật trạng thái chọn.");
                        ApplyCombinedFilters();
                    }
                    catch (Exception ex)
                    {
                        Log("Lỗi khi nhập Excel: " + ex.Message);
                        MessageBox.Show("Lỗi khi nhập Excel: \n" + ex.ToString(), "Lỗi Nhập Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        SetControlsEnabled(true);
                    }
                }
            }
        }

        private void btnClose_Click_1(object sender, EventArgs e)
        {
            Close();
        }
    }
}