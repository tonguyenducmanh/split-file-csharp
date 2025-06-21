using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LongFileChecker.Models;
using LongFileChecker.Services;
using FileToolLib.Services;

namespace LongFileChecker.Forms
{
    public partial class PreviewUnusedForm : Form
    {
        private DataGridView dgvUnused;
        private Button btnOK;
        private Button btnCancel;
        private Button btnExportExcel;
        private CheckBox chkSelectAll;
        private Panel bottomPanel;
        private ContextMenuStrip cmsRowMenu;
        private ToolStripMenuItem tsmiCopyDetails;
        public List<UnusedItemInfo> SelectedItems { get; private set; }

        public PreviewUnusedForm(List<UnusedItemInfo> items)
        {
            InitializeComponents();
            LoadData(items);
        }

        private void InitializeComponents()
        {
            this.Text = "Xem trước các phần tử không sử dụng";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 400);

            bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            chkSelectAll = new CheckBox
            {
                Text = "Chọn tất cả",
                AutoSize = true,
                Checked = true,
                Width = 100,
                Left = 10
            };
            chkSelectAll.Top = (bottomPanel.Height - chkSelectAll.Height) / 2;
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 30)
            };

            btnOK = new Button
            {
                Text = "Xóa đã chọn",
                DialogResult = DialogResult.OK,
                Size = new Size(100, 30)
            };

            btnExportExcel = new Button
            {
                Text = "Xuất Excel",
                Size = new Size(100, 30)
            };
            btnExportExcel.Click += BtnExportExcel_Click;

            // Đặt vị trí các nút
            btnCancel.Anchor = AnchorStyles.Right;
            btnOK.Anchor = AnchorStyles.Right;
            btnExportExcel.Anchor = AnchorStyles.Right;

            btnCancel.Top = (bottomPanel.Height - btnCancel.Height) / 2;
            btnOK.Top = (bottomPanel.Height - btnOK.Height) / 2;
            btnExportExcel.Top = (bottomPanel.Height - btnExportExcel.Height) / 2;

            bottomPanel.SizeChanged += (s, ev) => 
            {
                btnCancel.Left = bottomPanel.Width - btnCancel.Width - 10;
                btnOK.Left = btnCancel.Left - btnOK.Width - 10;
                btnExportExcel.Left = btnOK.Left - btnExportExcel.Width - 10;
            };

            bottomPanel.Controls.AddRange(new Control[] { chkSelectAll, btnExportExcel, btnOK, btnCancel });

            // Khởi tạo ContextMenuStrip
            tsmiCopyDetails = new ToolStripMenuItem("Copy Details");
            tsmiCopyDetails.Click += TsmiCopyDetails_Click;
            cmsRowMenu = new ContextMenuStrip();
            cmsRowMenu.Items.Add(tsmiCopyDetails);

            dgvUnused = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false,
                ReadOnly = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
                ContextMenuStrip = cmsRowMenu
            };

            var container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            container.Controls.Add(dgvUnused);

            this.Controls.AddRange(new Control[] { container, bottomPanel });

            InitializeGrid();

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Sự kiện để chọn dòng khi chuột phải
            dgvUnused.CellMouseDown += DgvUnused_CellMouseDown;
        }

        private void InitializeGrid()
        {
            dgvUnused.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewCheckBoxColumn
                {
                    Name = "Selected",
                    HeaderText = "",
                    Width = 30,
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "File",
                    HeaderText = "File",
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Class",
                    HeaderText = "Class",
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Item",
                    HeaderText = "Tên phần tử",
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Type",
                    HeaderText = "Loại",
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "Length",
                    HeaderText = "Độ dài (ký tự)",
                    ReadOnly = false
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "AccessModifier",
                    HeaderText = "Access Modifier",
                    ReadOnly = false
                }
            });
        }

        private void LoadData(List<UnusedItemInfo> items)
        {
            dgvUnused.Rows.Clear();
            foreach (var item in items)
            {
                int rowIndex = dgvUnused.Rows.Add();
                var row = dgvUnused.Rows[rowIndex];
                row.Cells["Selected"].Value = true;
                row.Cells["File"].Value = Path.GetFileName(item.FilePath);
                row.Cells["Class"].Value = item.ClassName;
                row.Cells["Item"].Value = item.ItemName;
                row.Cells["Type"].Value = item.Type;
                row.Cells["Length"].Value = item.Length.ToString("N0");
                row.Cells["AccessModifier"].Value = item.AccessModifier;
                row.Tag = item;
            }
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvUnused.Rows)
            {
                row.Cells["Selected"].Value = chkSelectAll.Checked;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                SelectedItems = new List<UnusedItemInfo>();
                foreach (DataGridViewRow row in dgvUnused.Rows)
                {
                    if ((bool)row.Cells["Selected"].Value)
                    {
                        var item = (UnusedItemInfo)row.Tag;
                        item.IsSelected = true;
                        SelectedItems.Add(item);
                    }
                }

                if (SelectedItems.Count == 0)
                {
                    e.Cancel = true;
                    MessageBox.Show("Vui lòng chọn ít nhất một phần tử để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            base.OnFormClosing(e);
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (dgvUnused.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                saveFileDialog.Title = "Chọn nơi lưu file Excel";
                saveFileDialog.DefaultExt = "xlsx";
                saveFileDialog.FileName = $"UnusedItemsPreview_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                string lastPath = MemoryBank.LoadPathSetting("LongFileChecker_ExportUnusedPreview_LastPath");
                if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
                {
                    saveFileDialog.InitialDirectory = lastPath;
                }
                else
                {
                    string defaultOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                    if (!Directory.Exists(defaultOutputDir)) Directory.CreateDirectory(defaultOutputDir);
                    saveFileDialog.InitialDirectory = defaultOutputDir;
                }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    MemoryBank.SavePathSetting("LongFileChecker_ExportUnusedPreview_LastPath", Path.GetDirectoryName(saveFileDialog.FileName));
                    string excelFilePath = saveFileDialog.FileName;

                    try
                    {
                        // Lấy tất cả các item đang hiển thị trong grid (bao gồm cả trạng thái selected của chúng)
                        List<UnusedItemInfo> itemsToExport = new List<UnusedItemInfo>();
                        foreach (DataGridViewRow row in dgvUnused.Rows)
                        {
                            if (row.Tag is UnusedItemInfo itemInfo)
                            {
                                // Cập nhật lại IsSelected từ checkbox trên grid trước khi xuất
                                if (row.Cells["Selected"].Value is bool isChecked)
                                {
                                    itemInfo.IsSelected = isChecked;
                                }
                                itemsToExport.Add(itemInfo);
                            }
                        }

                        if (!itemsToExport.Any())
                        {
                             MessageBox.Show("Không có item nào để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                             return;
                        }

                        ExcelExporter exporter = new ExcelExporter();
                        exporter.ExportUnusedItemsPreview(itemsToExport, excelFilePath);

                        // Mở file Excel sau khi tạo xong
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = excelFilePath,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);

                        MessageBox.Show($"Đã xuất dữ liệu ra file: {excelFilePath}\nFile sẽ được tự động mở.",
                                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Sự kiện cho ContextMenuStrip item
        private void TsmiCopyDetails_Click(object sender, EventArgs e)
        {
            if (dgvUnused.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dgvUnused.SelectedRows[0];
                if (selectedRow.Tag is UnusedItemInfo itemInfo)
                {
                    string detailsToCopy = $"File: {Path.GetFileName(itemInfo.FilePath)}\n" +
                                           $"Class: {itemInfo.ClassName}\n" +
                                           $"Item: {itemInfo.ItemName}\n" +
                                           $"Type: {itemInfo.Type}";
                    try
                    {
                        Clipboard.SetText(detailsToCopy);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi copy vào clipboard: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Sự kiện để chọn dòng khi chuột phải và hiển thị ContextMenu
        private void DgvUnused_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvUnused.ClearSelection();
                dgvUnused.Rows[e.RowIndex].Selected = true;
                // Không cần gán CurrentCell vì ContextMenuStrip sẽ hoạt động trên dòng được chọn
                // cmsRowMenu.Show(dgvUnused, e.Location); // Không cần thiết nếu đã gán vào ContextMenuStrip của dgv
            }
        }
    }
}