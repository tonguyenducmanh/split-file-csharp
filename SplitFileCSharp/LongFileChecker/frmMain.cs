using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using LongFileChecker.Models;
using LongFileChecker.Services;
using LongFileChecker.Forms;
using OfficeOpenXml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SplitFile;
using FileToolLib.Services;
using RemoveUnusedMember;

namespace LongFileChecker
{
    public partial class frmMain : Form
    {
        #region "Declare"
        private const string OUTPUT_FOLDER = "Output";
        private const string DEFAULT_PATTERN = "*.cs";
        private readonly CodeAnalyzerLongFile _codeAnalyzer;
        private readonly ExcelExporter _excelExporter;
        private CancellationTokenSource _cancellationTokenSource;
        private List<FileData> _foundFiles;
        private List<string> _patterns;
        #endregion

        #region "Construct"
        public frmMain()
        {
            InitializeComponent();
            _codeAnalyzer = new CodeAnalyzerLongFile();
            _excelExporter = new ExcelExporter();
            numMaxLength.Value = 200000; // Giá trị mặc định 200000 ký tự

            _foundFiles = new List<FileData>();
            _patterns = MemoryBank.LoadPatterns();

            // Thêm *.cs vào đầu tiên nếu chưa có trong patterns
            if (!_patterns.Contains(DEFAULT_PATTERN))
            {
                _patterns.Insert(0, DEFAULT_PATTERN);
                MemoryBank.SavePatterns(_patterns);
            }

            cboFilePattern.Items.Clear();
            foreach (var pattern in _patterns)
            {
                cboFilePattern.Items.Add(pattern);
            }

            // Luôn chọn *.cs làm mặc định
            cboFilePattern.Text = DEFAULT_PATTERN;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Đổi tên nút Clear thành "Xóa log"
            btnClear.Text = "Xóa log";

            // Đăng ký sự kiện cập nhật tiến độ từ CodeAnalyzerLongFile
            _codeAnalyzer.OnProgressUpdate += CodeAnalyzer_OnProgressUpdate;
        }
        #endregion

        #region "Sub/Function"
        private void CodeAnalyzer_OnProgressUpdate(string message)
        {
            if (txtResult.InvokeRequired)
            {
                txtResult.Invoke(new Action(() => AppendProgressMessage(message)));
            }
            else
            {
                AppendProgressMessage(message);
            }
        }

        private void AppendProgressMessage(string message)
        {
            if (txtResult.IsDisposed) return;
            txtResult.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtResult.ScrollToCaret(); // Tự động cuộn xuống dòng mới nhất
        }

        //private void ShowUnusedSummary()
        //{
        //    if (chkFindUnused.Checked)
        //    {
        //        var unusedSummary = _foundFiles
        //            .Where(f => f.CodeItems?.Any(c => !c.IsUsed && c.AccessModifier.Contains("private")) ?? false)
        //            .Select(f => new
        //            {
        //                File = f.Path,
        //                UnusedItems = f.CodeItems.Where(c => !c.IsUsed && c.AccessModifier.Contains("private")).ToList()
        //            })
        //            .Where(x => x.UnusedItems.Any())
        //            .ToList();

        //        if (unusedSummary.Any())
        //        {
        //            txtResult.AppendText("\r\n=== THỐNG KÊ THÀNH PHẦN KHÔNG SỬ DỤNG ===\r\n");
        //            txtResult.AppendText($"\r\nTổng số file có thành phần không sử dụng: {unusedSummary.Count}\r\n");

        //            foreach (var file in unusedSummary)
        //            {
        //                txtResult.AppendText($"\r\nFile: {Path.GetFileName(file.File)}\r\n");
        //                txtResult.AppendText($"Số thành phần không sử dụng: {file.UnusedItems.Count}\r\n");
        //                foreach (var item in file.UnusedItems)
        //                {
        //                    string info = $"  - {item.Name}";
        //                    if (item.Type == "Method")
        //                    {
        //                        info += $" [{item.ReturnType}]";
        //                        if (!string.IsNullOrEmpty(item.Parameters))
        //                            info += $" ({item.Parameters})";
        //                    }
        //                    info += $" ({item.Type})";
        //                    txtResult.AppendText($"{info}\r\n");
        //                }
        //            }
        //            txtResult.AppendText("\r\n==========================================\r\n");
        //        }
        //    }
        //}


        private async Task ScanFolder()
        {
            try
            {
                DisableControls();
                _foundFiles.Clear();
                txtResult.Clear(); 
                CodeAnalyzer_OnProgressUpdate("Bắt đầu quá trình quét thư mục...");

                var pattern = cboFilePattern.Text;
                if (!string.IsNullOrWhiteSpace(pattern) && !_patterns.Contains(pattern))
                {
                    _patterns.Add(pattern);
                    cboFilePattern.Items.Add(pattern);
                    MemoryBank.SavePatterns(_patterns);
                }

                _cancellationTokenSource = new CancellationTokenSource();

                string folderPath = txtFolderPath.Text;
                bool performDetailedAnalysis = chkDetailAnalysis.Checked;
                long maxLengthThreshold = Convert.ToInt64(numMaxLength.Value);

                CodeAnalyzer_OnProgressUpdate($"Đang quét thư mục: {folderPath} với pattern: {pattern}");
                CodeAnalyzer_OnProgressUpdate($"Chế độ phân tích chi tiết file: {performDetailedAnalysis}");
                CodeAnalyzer_OnProgressUpdate($"Chỉ xử lý file có kích thước lớn hơn: {maxLengthThreshold} bytes (0 = không giới hạn).");

                // Updated call to AnalyzeDirectoryAsync
                _foundFiles = await _codeAnalyzer.AnalyzeDirectoryAsync(folderPath, pattern, performDetailedAnalysis, maxLengthThreshold, _cancellationTokenSource.Token);

                // Sau khi AnalyzeDirectoryAsync hoàn tất và trả về _foundFiles:
                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested) // Kiểm tra token nếu được implement
                {
                    CodeAnalyzer_OnProgressUpdate("Hoàn tất phân tích thư mục. Đang hiển thị kết quả...");
                    ShowCompletionMessage(); // Hiển thị thông báo hoàn thành và kết quả tóm tắt
                    //ShowUnusedSummary();     // Hiển thị thống kê thành phần không sử dụng nếu có
                }
                else if (_cancellationTokenSource != null && _cancellationTokenSource.Token.IsCancellationRequested)
                {
                    CodeAnalyzer_OnProgressUpdate("Quá trình quét đã bị hủy bởi người dùng.");
                }
            }
            catch (OperationCanceledException) // Bắt lỗi nếu AnalyzeDirectoryAsync hỗ trợ và ném lỗi này
            {
                CodeAnalyzer_OnProgressUpdate("Đã dừng quét file!");
            }
            catch (Exception ex)
            {
                CodeAnalyzer_OnProgressUpdate($"Lỗi nghiêm trọng khi quét thư mục: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EnableControls();
                 _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private List<UnusedItemInfo> GetUnusedItemsForPreview()
        {
            CodeAnalyzer_OnProgressUpdate("Bắt đầu thu thập các thành phần không sử dụng để review...");
            var items = _foundFiles
                .Where(f => f.CodeItems != null)
                .SelectMany(f => f.CodeItems
                    .Where(c => !c.IsUsed && 
                                (c.AccessModifier.Contains("private") || 
                                 c.AccessModifier.Contains("internal") || 
                                 c.AccessModifier.Contains("protected internal")) && // Mở rộng cho internal và protected internal
                                !string.IsNullOrEmpty(c.Name) && // Đảm bảo có tên
                                c.OriginalStartOffset > 0 && c.OriginalFullSpanLength > 0) // Đảm bảo có offset và length
                    .Select(c => new UnusedItemInfo
                    {
                        FilePath = f.Path,
                        // Giả định c.Name có dạng Class.Member hoặc chỉ Member (nếu là class)
                        ClassName = c.ClassName, // Cần đảm bảo CodeItemLongFile có trường này
                        ItemName = c.Name.Contains(".") ? c.Name.Split('.').Last() : c.Name, 
                        Type = c.Type,
                        Length = c.Length, // Dùng Length từ CodeItemLongFile (FullSpan.Length)
                        AccessModifier = c.AccessModifier,
                        // Lưu lại thông tin gốc để tạo lại CodeItemLongFile khi cần xóa
                        OriginalStart = c.OriginalStartOffset,
                        OriginalLength = c.OriginalFullSpanLength,
                        CodeItemReference = c // Giữ tham chiếu đến CodeItemLongFile gốc
                    }))
                .ToList();
            CodeAnalyzer_OnProgressUpdate($"Tìm thấy {items.Count} thành phần có thể không sử dụng để review.");
            return items;
        }

        private async Task RemoveUnusedMembers()
        {
            if (_foundFiles == null || !_foundFiles.Any())
            {
                MessageBox.Show("Chưa có dữ liệu phân tích. Vui lòng quét thư mục trước.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var unusedItemsForPreview = GetUnusedItemsForPreview();
            if (!unusedItemsForPreview.Any())
            {
                MessageBox.Show("Không tìm thấy thành phần nào (private, internal, protected internal) không được sử dụng để xóa.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hiện tại PreviewUnusedForm sử dụng UnusedItemInfo.
            // Chúng ta sẽ truyền UnusedItemInfo và sau đó lấy lại CodeItemLongFile từ tham chiếu.
            using (var previewForm = new PreviewUnusedForm(unusedItemsForPreview)) 
            {
                CodeAnalyzer_OnProgressUpdate("Hiển thị form preview các thành phần không sử dụng...");
                if (previewForm.ShowDialog() == DialogResult.OK && previewForm.SelectedItems?.Any() == true)
                {
                    var selectedUnusedInfoItems = previewForm.SelectedItems;
                    // Chuyển đổi các UnusedItemInfo đã chọn trở lại CodeItemLongFile
                    // bằng cách sử dụng tham chiếu CodeItemReference đã lưu.
                    var itemsToActuallyRemove = selectedUnusedInfoItems
                        .Select(info => info.CodeItemReference) // info.CodeItemReference là CodeItemLongFile gốc
                        .Where(ci => ci != null) // Đảm bảo không null
                        .ToList();

                    if (itemsToActuallyRemove.Any())
                    {
                        CodeAnalyzer_OnProgressUpdate($"Chuẩn bị xóa {itemsToActuallyRemove.Count} thành phần đã chọn...");
                        DisableControls();
                        // Create a new CancellationTokenSource for the remove operation
                        using (var removeCts = new CancellationTokenSource()) 
                        {
                            try
                            {
                                // Explicitly type the deconstruction variables
                                (bool success, string message, List<string> modifiedFiles) = await _codeAnalyzer.RemoveMembersAsync(itemsToActuallyRemove, previewOnly: false, removeCts.Token); // Pass token
                                CodeAnalyzer_OnProgressUpdate(message); // Use CodeAnalyzer_OnProgressUpdate or AppendProgressMessage

                                if (success)
                                {
                                    MessageBox.Show($"Đã xử lý xong việc xóa {itemsToActuallyRemove.Count} thành phần. Số file bị thay đổi: {modifiedFiles.Count}.\nChi tiết xem ở log.",
                                        "Hoàn thành", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                
                                    CodeAnalyzer_OnProgressUpdate("Quét lại thư mục để cập nhật kết quả sau khi xóa...");
                                    await ScanFolder(); 
                                }
                                else
                                {
                                    MessageBox.Show($"Có lỗi trong quá trình xóa: {message}",
                                        "Lỗi xóa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            catch (OperationCanceledException ex) // Catch cancellation specific to remove operation
                            {
                                CodeAnalyzer_OnProgressUpdate($"Hoạt động xóa thành viên đã bị hủy: {ex.Message}");
                                MessageBox.Show("Hoạt động xóa thành viên đã bị hủy.", "Đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            catch (Exception ex)
                            {
                                CodeAnalyzer_OnProgressUpdate($"Lỗi nghiêm trọng khi thực hiện RemoveMembersAsync: {ex.Message}\n{ex.StackTrace}");
                                MessageBox.Show($"Lỗi nghiêm trọng khi xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            finally
                            {
                                EnableControls(); 
                            }
                        }
                    }
                    else
                    {
                        CodeAnalyzer_OnProgressUpdate("Không có thành phần nào được chọn để xóa sau preview.");
                    }
                }
                else
                {
                     CodeAnalyzer_OnProgressUpdate("Người dùng đã hủy hoặc không chọn thành phần nào từ form preview.");
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                string lastPath = MemoryBank.LoadPathSetting("Main_BrowseFolder_LastPath");
                if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                {
                    dialog.SelectedPath = lastPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    MemoryBank.SavePathSetting("Main_BrowseFolder_LastPath", dialog.SelectedPath);
                    txtFolderPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtResult.Clear();
            _foundFiles.Clear();
            btnExportExcel.Enabled = false;
            //btnRemoveUnused.Enabled = false;
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("Vui lòng chọn một thư mục hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Dispose previous CancellationTokenSource if it exists and is not null
            _cancellationTokenSource?.Dispose(); 
            _cancellationTokenSource = new CancellationTokenSource();
            btnStop.Enabled = true;
            btnScan.Enabled = false;
            DisableControls(); // Should be called after setting btnScan.Enabled = false; and btnStop.Enabled = true;

            try
            {
                await ScanFolder(); // ScanFolder now uses _cancellationTokenSource.Token internally
                                    // And ShowCompletionMessage is called within ScanFolder or its catch blocks
            }
            // Catching OperationCanceledException here is for cancellation initiated 
            // by _cancellationTokenSource for the ScanFolder operation.
            catch (OperationCanceledException) 
            {
                // This message might be redundant if ScanFolder already handles it and calls ShowCompletionMessage(true)
                AppendProgressMessage("Quá trình quét chính đã bị hủy."); 
                // ShowCompletionMessage(true) is likely called within ScanFolder's finally or catch already.
            }
            catch (Exception ex)
            {
                AppendProgressMessage($"Lỗi trong quá trình quét: {ex.Message}");
                MessageBox.Show($"Lỗi trong quá trình quét: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // EnableControls() is called in ScanFolder's finally block.
                // We only manage the state of btnScan and btnStop here, 
                // as DisableControls/EnableControls handle the rest.
                btnScan.Enabled = true;
                btnStop.Enabled = false;
                _cancellationTokenSource?.Dispose(); // Ensure disposal
                _cancellationTokenSource = null;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                //UpdateProgress("Đang yêu cầu dừng...");
                btnStop.Enabled = false;
            }
        }

        private async void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (_foundFiles.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                var defaultOutputPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath),
                    OUTPUT_FOLDER
                );
                Directory.CreateDirectory(defaultOutputPath);

                dialog.Filter = "Excel Files|*.xlsx";
                dialog.DefaultExt = "xlsx";
                dialog.AddExtension = true;
                dialog.FileName = Path.GetFileName(txtFolderPath.Text) + "_Analyze";

                string lastPath = MemoryBank.LoadPathSetting("Main_ExportExcel_LastPath");
                if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                {
                    dialog.InitialDirectory = lastPath;
                }
                else
                {
                    dialog.InitialDirectory = defaultOutputPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        MemoryBank.SavePathSetting("Main_ExportExcel_LastPath", Path.GetDirectoryName(dialog.FileName));

                        await _excelExporter.ExportToExcelAsync(dialog.FileName, _foundFiles);

                        MessageBox.Show("Đã xuất file thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void btnRemoveUnused_Click(object sender, EventArgs e)
        {
            var form = new MainForm();
            form.Show();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DisableControls()
        {
            btnScan.Enabled = false;
            btnBrowse.Enabled = false;
            btnStop.Enabled = true;
            btnExportExcel.Enabled = false;
            btnClear.Enabled = false;
            //btnRemoveUnused.Enabled = false;
        }

        private void EnableControls()
        {
            btnScan.Enabled = true;
            btnBrowse.Enabled = true;
            btnStop.Enabled = false;
            btnClear.Enabled = true;
            btnExportExcel.Enabled = _foundFiles.Count > 0;
            //btnRemoveUnused.Enabled = _foundFiles.Any(f => f.CodeItems?.Any(c => !c.IsUsed && c.AccessModifier.Contains("private")) ?? false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void ShowCompletionMessage(bool cancelled = false)
        {
            if (cancelled)
            {
                AppendProgressMessage("Quá trình quét đã bị dừng bởi người dùng.");
                MessageBox.Show("Quá trình quét đã bị dừng.", "Đã dừng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string message;
            if (_foundFiles == null || !_foundFiles.Any())
            {
                message = "Không tìm thấy file nào thỏa mãn điều kiện.";
                AppendProgressMessage("Hoàn tất. Không tìm thấy file.");
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Tìm thấy {_foundFiles.Count} file thỏa mãn:");
                sb.AppendLine("---");
                sb.AppendLine($"Thư mục quét: {txtFolderPath.Text}");
                sb.AppendLine($"Pattern: {cboFilePattern.Text}");
                sb.AppendLine($"Phân tích chi tiết: {(chkDetailAnalysis.Checked ? "Có" : "Không")}");
                sb.AppendLine($"Ngưỡng kích thước tối thiểu (bytes): {(numMaxLength.Value > 0 ? numMaxLength.Value.ToString() : "Không áp dụng")}");
                sb.AppendLine("---");

                if (!chkDetailAnalysis.Checked)
                {
                    // Display format: FilePath (Length ký tự)
                    foreach (var fileData in _foundFiles.OrderByDescending(f => f.Length))
                    {
                        sb.AppendLine($"{fileData.Path} ({fileData.Length:N0} ký tự)");
                    }
                }
                else
                {
                    // Detailed analysis display (existing logic)
                    int fileCount = 0;
                    foreach (var fileData in _foundFiles.OrderByDescending(f => f.Length))
                    {
                        fileCount++;
                        sb.AppendLine($"{fileCount}. File: {fileData.Path} ({fileData.Length:N0} bytes)");
                        if (fileData.CodeItems != null && fileData.CodeItems.Any())
                        {
                            foreach (var item in fileData.CodeItems)
                            {
                                sb.AppendLine($"    - {item.Type} {item.Name} (Length: {item.Length}, Modifier: {item.AccessModifier}, Used: {item.IsUsed})");
                                if (item.IsMethod)
                                {
                                    sb.AppendLine($"        Return: {item.ReturnType}, Params: {item.Parameters}");
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine("    (Không có thành phần code nào được phân tích chi tiết hoặc không có thành phần nào.)");
                        }
                        sb.AppendLine();
                    }
                }
                message = sb.ToString();
                AppendProgressMessage($"Hoàn tất. Tìm thấy {_foundFiles.Count} file.");
            }
            
            txtResult.Text = message;
            MessageBox.Show("Quá trình quét đã hoàn tất!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // tabControl.SelectedTab = tabPageResults; // Commented out as these controls might not exist
        }
        #endregion

        private void btnSplitFile_Click(object sender, EventArgs e)
        {
            var form = new frmSplitFile();
            form.Show();
        }
    }
}