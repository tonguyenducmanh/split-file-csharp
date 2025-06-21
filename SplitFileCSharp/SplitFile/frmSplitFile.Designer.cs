namespace SplitFile
{
    partial class frmSplitFile
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lstFiles = new ListView();
            colFilePath = new ColumnHeader();
            colListIsMain = new ColumnHeader();
            btnAddFiles = new Button();
            btnSetMainFile = new Button();
            btnRemoveFiles = new Button();
            btnAnalyze = new Button();
            txtResult = new TextBox();
            label1 = new Label();
            dgvSplitConfig = new DataGridView();
            colOriginalFile = new DataGridViewTextBoxColumn();
            colIsMain = new DataGridViewComboBoxColumn();
            colMainFile = new DataGridViewTextBoxColumn();
            colFileName = new DataGridViewTextBoxColumn();
            colDescription = new DataGridViewTextBoxColumn();
            colMethods = new DataGridViewTextBoxColumn();
            btnSplit = new Button();
            btnClose = new Button();
            btnRemoveUnused = new Button();
            btnImportConfig = new Button();
            btnExportConfig = new Button();
            label2 = new Label();
            label3 = new Label();
            btnExportExcel = new Button();
            btnClearLog = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvSplitConfig).BeginInit();
            SuspendLayout();
            // 
            // lstFiles
            // 
            lstFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lstFiles.Columns.AddRange(new ColumnHeader[] { colFilePath, colListIsMain });
            lstFiles.FullRowSelect = true;
            lstFiles.GridLines = true;
            lstFiles.Location = new Point(12, 38);
            lstFiles.Name = "lstFiles";
            lstFiles.Size = new Size(946, 100);
            lstFiles.TabIndex = 0;
            lstFiles.UseCompatibleStateImageBehavior = false;
            lstFiles.View = View.Details;
            // 
            // colFilePath
            // 
            colFilePath.Text = "Đường dẫn";
            colFilePath.Width = 300;
            // 
            // colListIsMain
            // 
            colListIsMain.Text = "File chính";
            colListIsMain.Width = 70;
            // 
            // btnAddFiles
            // 
            btnAddFiles.Location = new Point(100, 9);
            btnAddFiles.Name = "btnAddFiles";
            btnAddFiles.Size = new Size(75, 23);
            btnAddFiles.TabIndex = 1;
            btnAddFiles.Text = "Thêm file";
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += btnAddFiles_Click;
            // 
            // btnSetMainFile
            // 
            btnSetMainFile.Location = new Point(181, 9);
            btnSetMainFile.Name = "btnSetMainFile";
            btnSetMainFile.Size = new Size(75, 23);
            btnSetMainFile.TabIndex = 2;
            btnSetMainFile.Text = "Đặt chính";
            btnSetMainFile.UseVisualStyleBackColor = true;
            btnSetMainFile.Click += btnSetMainFile_Click;
            // 
            // btnRemoveFiles
            // 
            btnRemoveFiles.Location = new Point(262, 9);
            btnRemoveFiles.Name = "btnRemoveFiles";
            btnRemoveFiles.Size = new Size(75, 23);
            btnRemoveFiles.TabIndex = 3;
            btnRemoveFiles.Text = "Xóa file";
            btnRemoveFiles.UseVisualStyleBackColor = true;
            btnRemoveFiles.Click += btnRemoveFiles_Click;
            // 
            // btnAnalyze
            // 
            btnAnalyze.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAnalyze.Location = new Point(883, 12);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(75, 23);
            btnAnalyze.TabIndex = 4;
            btnAnalyze.Text = "Phân tích";
            btnAnalyze.UseVisualStyleBackColor = true;
            btnAnalyze.Click += btnAnalyze_Click;
            // 
            // txtResult
            // 
            txtResult.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtResult.Location = new Point(12, 454);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Both;
            txtResult.Size = new Size(944, 178);
            txtResult.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(89, 15);
            label1.TabIndex = 6;
            label1.Text = "Danh sách files:";
            // 
            // dgvSplitConfig
            // 
            dgvSplitConfig.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvSplitConfig.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSplitConfig.Columns.AddRange(new DataGridViewColumn[] { colOriginalFile, colIsMain, colMainFile, colFileName, colDescription, colMethods });
            dgvSplitConfig.Location = new Point(12, 174);
            dgvSplitConfig.Name = "dgvSplitConfig";
            dgvSplitConfig.Size = new Size(944, 247);
            dgvSplitConfig.TabIndex = 7;
            // 
            // colOriginalFile
            // 
            colOriginalFile.HeaderText = "Đường dẫn file gốc";
            colOriginalFile.Name = "colOriginalFile";
            colOriginalFile.Width = 200;
            // 
            // colIsMain
            // 
            colIsMain.HeaderText = "File chính";
            colIsMain.Name = "colIsMain";
            colIsMain.Width = 80;
            // 
            // colMainFile
            // 
            colMainFile.HeaderText = "Đường dẫn file chính";
            colMainFile.Name = "colMainFile";
            colMainFile.Width = 200;
            // 
            // colFileName
            // 
            colFileName.HeaderText = "Tên file mới";
            colFileName.Name = "colFileName";
            colFileName.Width = 120;
            // 
            // colDescription
            // 
            colDescription.HeaderText = "Mô tả";
            colDescription.Name = "colDescription";
            colDescription.Width = 150;
            // 
            // colMethods
            // 
            colMethods.HeaderText = "Các methods";
            colMethods.Name = "colMethods";
            colMethods.Width = 150;
            // 
            // btnSplit
            // 
            btnSplit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSplit.Location = new Point(731, 641);
            btnSplit.Name = "btnSplit";
            btnSplit.Size = new Size(75, 23);
            btnSplit.TabIndex = 13;
            btnSplit.Text = "Tách file";
            btnSplit.UseVisualStyleBackColor = true;
            btnSplit.Click += btnSplit_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(893, 641);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(75, 23);
            btnClose.TabIndex = 15;
            btnClose.Text = "Đóng";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnRemoveUnused
            // 
            btnRemoveUnused.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRemoveUnused.Location = new Point(812, 641);
            btnRemoveUnused.Name = "btnRemoveUnused";
            btnRemoveUnused.Size = new Size(75, 23);
            btnRemoveUnused.TabIndex = 14;
            btnRemoveUnused.Text = "Xóa thừa";
            btnRemoveUnused.UseVisualStyleBackColor = true;
            //btnRemoveUnused.Click += btnRemoveUnused_Click;
            // 
            // btnImportConfig
            // 
            btnImportConfig.Location = new Point(343, 9);
            btnImportConfig.Name = "btnImportConfig";
            btnImportConfig.Size = new Size(75, 23);
            btnImportConfig.TabIndex = 9;
            btnImportConfig.Text = "Nhập Excel";
            btnImportConfig.UseVisualStyleBackColor = true;
            btnImportConfig.Click += btnImportConfig_Click;
            // 
            // btnExportConfig
            // 
            btnExportConfig.Location = new Point(424, 9);
            btnExportConfig.Name = "btnExportConfig";
            btnExportConfig.Size = new Size(75, 23);
            btnExportConfig.TabIndex = 10;
            btnExportConfig.Text = "Tải mẫu";
            btnExportConfig.UseVisualStyleBackColor = true;
            btnExportConfig.Click += btnExportConfig_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 142);
            label2.Name = "label2";
            label2.Size = new Size(103, 15);
            label2.TabIndex = 8;
            label2.Text = "Cấu hình tách file:";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Location = new Point(12, 436);
            label3.Name = "label3";
            label3.Size = new Size(47, 15);
            label3.TabIndex = 14;
            label3.Text = "Kết quả";
            // 
            // btnExportExcel
            // 
            btnExportExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExportExcel.Location = new Point(640, 641);
            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Size = new Size(85, 23);
            btnExportExcel.TabIndex = 12;
            btnExportExcel.Text = "Xuất Excel";
            btnExportExcel.UseVisualStyleBackColor = true;
            // 
            // btnClearLog
            // 
            btnClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClearLog.Location = new Point(554, 641);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(80, 23);
            btnClearLog.TabIndex = 11;
            btnClearLog.Text = "Xóa log";
            btnClearLog.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(965, 673);
            Controls.Add(label3);
            Controls.Add(btnClose);
            Controls.Add(btnRemoveUnused);
            Controls.Add(btnSplit);
            Controls.Add(btnExportConfig);
            Controls.Add(btnImportConfig);
            Controls.Add(label2);
            Controls.Add(dgvSplitConfig);
            Controls.Add(label1);
            Controls.Add(txtResult);
            Controls.Add(btnAnalyze);
            Controls.Add(btnRemoveFiles);
            Controls.Add(btnSetMainFile);
            Controls.Add(btnAddFiles);
            Controls.Add(lstFiles);
            Controls.Add(btnExportExcel);
            Controls.Add(btnClearLog);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Split File Tool";
            ((System.ComponentModel.ISupportInitialize)dgvSplitConfig).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView lstFiles;
        private ColumnHeader colFilePath;
        private ColumnHeader colListIsMain;
        private Button btnAddFiles;
        private Button btnSetMainFile;
        private Button btnRemoveFiles;
        private Button btnAnalyze;
        private TextBox txtResult;
        private Label label1;
        private Label label2;
        private DataGridView dgvSplitConfig;
        private Button btnImportConfig;
        private Button btnExportConfig;
        private Button btnSplit;
        private Button btnClose;
        private Button btnRemoveUnused;
        private DataGridViewTextBoxColumn colOriginalFile;
        private DataGridViewComboBoxColumn colIsMain;
        private DataGridViewTextBoxColumn colMainFile;
        private DataGridViewTextBoxColumn colFileName;
        private DataGridViewTextBoxColumn colDescription;
        private DataGridViewTextBoxColumn colMethods;
        private Label label3;
        private Button btnExportExcel;
        private Button btnClearLog;
    }
}