namespace LongFileChecker
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            txtFolderPath = new TextBox();
            btnBrowse = new Button();
            label1 = new Label();
            numMaxLength = new NumericUpDown();
            label2 = new Label();
            txtResult = new TextBox();
            label3 = new Label();
            btnScan = new Button();
            btnStop = new Button();
            btnExportExcel = new Button();
            btnClose = new Button();
            btnClear = new Button();
            cboFilePattern = new ComboBox();
            label4 = new Label();
            toolTip = new ToolTip(components);
            chkDetailAnalysis = new CheckBox();
            btnRemoveUnused = new Button();
            btnSplitFile = new Button();
            ((System.ComponentModel.ISupportInitialize)numMaxLength).BeginInit();
            SuspendLayout();
            // 
            // txtFolderPath
            // 
            txtFolderPath.Location = new Point(157, 12);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(502, 23);
            txtFolderPath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(665, 12);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(107, 23);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "Chọn thư mục";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(114, 15);
            label1.TabIndex = 2;
            label1.Text = "Đường dẫn thư mục";
            // 
            // numMaxLength
            // 
            numMaxLength.Location = new Point(157, 41);
            numMaxLength.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numMaxLength.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxLength.Name = "numMaxLength";
            numMaxLength.Size = new Size(120, 23);
            numMaxLength.TabIndex = 3;
            numMaxLength.Value = new decimal(new int[] { 16000, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 43);
            label2.Name = "label2";
            label2.Size = new Size(116, 15);
            label2.TabIndex = 4;
            label2.Text = "Số lượng ký tự tối đa";
            // 
            // txtResult
            // 
            txtResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtResult.Location = new Point(12, 125);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Both;
            txtResult.Size = new Size(760, 274);
            txtResult.TabIndex = 7;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 107);
            label3.Name = "label3";
            label3.Size = new Size(47, 15);
            label3.TabIndex = 8;
            label3.Text = "Kết quả";
            // 
            // btnScan
            // 
            btnScan.Location = new Point(373, 405);
            btnScan.Name = "btnScan";
            btnScan.Size = new Size(75, 23);
            btnScan.TabIndex = 9;
            btnScan.Text = "Tìm file dài";
            btnScan.UseVisualStyleBackColor = true;
            btnScan.Click += btnScan_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new Point(454, 405);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(75, 23);
            btnStop.TabIndex = 10;
            btnStop.Text = "Dừng";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnExportExcel
            // 
            btnExportExcel.Location = new Point(616, 405);
            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Size = new Size(75, 23);
            btnExportExcel.TabIndex = 12;
            btnExportExcel.Text = "Xuất Excel";
            btnExportExcel.UseVisualStyleBackColor = true;
            btnExportExcel.Click += btnExportExcel_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(697, 405);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(75, 23);
            btnClose.TabIndex = 13;
            btnClose.Text = "Đóng";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnClear
            // 
            btnClear.Location = new Point(535, 405);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(75, 23);
            btnClear.TabIndex = 11;
            btnClear.Text = "Xóa log";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // cboFilePattern
            // 
            cboFilePattern.FormattingEnabled = true;
            cboFilePattern.Items.AddRange(new object[] { "*.*", "*.cs", "!*\\bin\\*;!*\\obj\\*;!*\\.*", "*.cs;*.json", "*.cs;*.cshtml;*.razor" });
            cboFilePattern.Location = new Point(157, 70);
            cboFilePattern.Name = "cboFilePattern";
            cboFilePattern.Size = new Size(250, 23);
            cboFilePattern.TabIndex = 5;
            cboFilePattern.Text = "*.*";
            toolTip.SetToolTip(cboFilePattern, "*.* = Tất cả các file\r\n*.cs = Chỉ file C#\r\n!*\\bin\\* = Loại trừ thư mục bin\r\n*.cs;*.json = File C# và JSON\r\nDấu ; để kết hợp nhiều pattern");
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 73);
            label4.Name = "label4";
            label4.Size = new Size(81, 15);
            label4.TabIndex = 6;
            label4.Text = "Định dạng file";
            // 
            // chkDetailAnalysis
            // 
            chkDetailAnalysis.AutoSize = true;
            chkDetailAnalysis.Location = new Point(157, 99);
            chkDetailAnalysis.Name = "chkDetailAnalysis";
            chkDetailAnalysis.Size = new Size(111, 19);
            chkDetailAnalysis.TabIndex = 14;
            chkDetailAnalysis.Text = "Chi tiết từng file";
            chkDetailAnalysis.UseVisualStyleBackColor = true;
            chkDetailAnalysis.Visible = false;
            // 
            // btnRemoveUnused
            // 
            btnRemoveUnused.Location = new Point(93, 406);
            btnRemoveUnused.Name = "btnRemoveUnused";
            btnRemoveUnused.Size = new Size(87, 23);
            btnRemoveUnused.TabIndex = 17;
            btnRemoveUnused.Text = "Xóa k/dùng";
            btnRemoveUnused.UseVisualStyleBackColor = true;
            btnRemoveUnused.Click += btnRemoveUnused_Click;
            // 
            // btnSplitFile
            // 
            btnSplitFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSplitFile.Location = new Point(12, 406);
            btnSplitFile.Name = "btnSplitFile";
            btnSplitFile.Size = new Size(75, 23);
            btnSplitFile.TabIndex = 18;
            btnSplitFile.Text = "Tách file";
            btnSplitFile.UseVisualStyleBackColor = true;
            btnSplitFile.Click += btnSplitFile_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 441);
            Controls.Add(btnSplitFile);
            Controls.Add(btnRemoveUnused);
            Controls.Add(chkDetailAnalysis);
            Controls.Add(btnClose);
            Controls.Add(btnExportExcel);
            Controls.Add(btnClear);
            Controls.Add(btnStop);
            Controls.Add(btnScan);
            Controls.Add(label3);
            Controls.Add(txtResult);
            Controls.Add(label4);
            Controls.Add(cboFilePattern);
            Controls.Add(label2);
            Controls.Add(numMaxLength);
            Controls.Add(label1);
            Controls.Add(btnBrowse);
            Controls.Add(txtFolderPath);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Kiểm tra độ dài file";
            ((System.ComponentModel.ISupportInitialize)numMaxLength).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFolderPath;
        private Button btnBrowse;
        private Label label1;
        private NumericUpDown numMaxLength;
        private Label label2;
        private ComboBox cboFilePattern;
        private Label label4;
        private TextBox txtResult;
        private Label label3;
        private Button btnScan;
        private Button btnStop;
        private Button btnClear;
        private Button btnExportExcel;
        private Button btnClose;
        private ToolTip toolTip;
        private CheckBox chkDetailAnalysis;
        private Button btnRemoveUnused;
        private Button btnSplitFile;
    }
}