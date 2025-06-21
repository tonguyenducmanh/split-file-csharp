namespace RemoveUnusedMember
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblFolderPath = new Label();
            txtFolderPath = new TextBox();
            btnSelectFolder = new Button();
            btnScan = new Button();
            dgvUnusedMembers = new DataGridView();
            colSelect = new DataGridViewCheckBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colAccessibility = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colFilePath = new DataGridViewTextBoxColumn();
            colLineNumber = new DataGridViewTextBoxColumn();
            colContainingType = new DataGridViewTextBoxColumn();
            lblResults = new Label();
            txtResults = new TextBox();
            btnDeleteSelected = new Button();
            btnRefresh = new Button();
            folderBrowserDialog = new FolderBrowserDialog();
            btnCopySelected = new Button();
            lblFilter = new Label();
            txtFilter = new TextBox();
            btnFilter = new Button();
            lblFilterType = new Label();
            cmbFilterType = new ComboBox();
            lblFilterAccessibility = new Label();
            cmbFilterAccessibility = new ComboBox();
            lblFilterFile = new Label();
            cmbFilterFile = new ComboBox();
            lblFilterClass = new Label();
            cmbFilterClass = new ComboBox();
            chkOnlyMethods = new CheckBox();
            btnExportExcel = new Button();
            btnImportExcel = new Button();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvUnusedMembers).BeginInit();
            SuspendLayout();
            // 
            // lblFolderPath
            // 
            lblFolderPath.AutoSize = true;
            lblFolderPath.Location = new Point(12, 15);
            lblFolderPath.Name = "lblFolderPath";
            lblFolderPath.Size = new Size(69, 15);
            lblFolderPath.TabIndex = 0;
            lblFolderPath.Text = "Đường dẫn:";
            // 
            // txtFolderPath
            // 
            txtFolderPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFolderPath.Location = new Point(88, 12);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(504, 23);
            txtFolderPath.TabIndex = 1;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectFolder.Location = new Point(598, 11);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(94, 23);
            btnSelectFolder.TabIndex = 2;
            btnSelectFolder.Text = "Chọn thư mục";
            btnSelectFolder.UseVisualStyleBackColor = true;
            // 
            // btnScan
            // 
            btnScan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnScan.Location = new Point(698, 11);
            btnScan.Name = "btnScan";
            btnScan.Size = new Size(75, 23);
            btnScan.TabIndex = 3;
            btnScan.Text = "Scan";
            btnScan.UseVisualStyleBackColor = true;
            // 
            // dgvUnusedMembers
            // 
            dgvUnusedMembers.AllowUserToAddRows = false;
            dgvUnusedMembers.AllowUserToDeleteRows = false;
            dgvUnusedMembers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvUnusedMembers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvUnusedMembers.Columns.AddRange(new DataGridViewColumn[] { colSelect, colType, colAccessibility, colName, colFilePath, colLineNumber, colContainingType });
            dgvUnusedMembers.Location = new Point(12, 128);
            dgvUnusedMembers.Name = "dgvUnusedMembers";
            dgvUnusedMembers.Size = new Size(761, 199);
            dgvUnusedMembers.TabIndex = 23;
            // 
            // colSelect
            // 
            colSelect.HeaderText = "Chọn";
            colSelect.Name = "colSelect";
            colSelect.Width = 50;
            // 
            // colType
            // 
            colType.HeaderText = "Loại";
            colType.Name = "colType";
            colType.ReadOnly = true;
            // 
            // colAccessibility
            // 
            colAccessibility.HeaderText = "Mức truy cập";
            colAccessibility.Name = "colAccessibility";
            // 
            // colName
            // 
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colName.HeaderText = "Tên";
            colName.Name = "colName";
            colName.ReadOnly = true;
            // 
            // colFilePath
            // 
            colFilePath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colFilePath.HeaderText = "Đường dẫn tệp";
            colFilePath.Name = "colFilePath";
            colFilePath.ReadOnly = true;
            // 
            // colLineNumber
            // 
            colLineNumber.HeaderText = "Dòng";
            colLineNumber.Name = "colLineNumber";
            colLineNumber.ReadOnly = true;
            colLineNumber.Width = 60;
            // 
            // colContainingType
            // 
            colContainingType.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colContainingType.HeaderText = "Lớp chứa";
            colContainingType.Name = "colContainingType";
            colContainingType.ReadOnly = true;
            // 
            // lblResults
            // 
            lblResults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblResults.AutoSize = true;
            lblResults.Location = new Point(12, 330);
            lblResults.Name = "lblResults";
            lblResults.Size = new Size(50, 15);
            lblResults.TabIndex = 5;
            lblResults.Text = "Kết quả:";
            // 
            // txtResults
            // 
            txtResults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtResults.Location = new Point(12, 348);
            txtResults.Multiline = true;
            txtResults.Name = "txtResults";
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Both;
            txtResults.Size = new Size(761, 70);
            txtResults.TabIndex = 24;
            // 
            // btnDeleteSelected
            // 
            btnDeleteSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDeleteSelected.Location = new Point(12, 454);
            btnDeleteSelected.Name = "btnDeleteSelected";
            btnDeleteSelected.Size = new Size(120, 23);
            btnDeleteSelected.TabIndex = 25;
            btnDeleteSelected.Text = "Xóa đã chọn";
            btnDeleteSelected.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRefresh.Location = new Point(138, 454);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(75, 23);
            btnRefresh.TabIndex = 26;
            btnRefresh.Text = "Làm mới";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnCopySelected
            // 
            btnCopySelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCopySelected.Location = new Point(219, 454);
            btnCopySelected.Name = "btnCopySelected";
            btnCopySelected.Size = new Size(100, 23);
            btnCopySelected.TabIndex = 27;
            btnCopySelected.Text = "Sao chép";
            btnCopySelected.UseVisualStyleBackColor = true;
            // 
            // lblFilter
            // 
            lblFilter.AutoSize = true;
            lblFilter.Location = new Point(12, 44);
            lblFilter.Name = "lblFilter";
            lblFilter.Size = new Size(29, 15);
            lblFilter.TabIndex = 11;
            lblFilter.Text = "Lọc:";
            // 
            // txtFilter
            // 
            txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilter.Location = new Point(48, 41);
            txtFilter.Name = "txtFilter";
            txtFilter.Size = new Size(644, 23);
            txtFilter.TabIndex = 12;
            // 
            // btnFilter
            // 
            btnFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFilter.Location = new Point(698, 40);
            btnFilter.Name = "btnFilter";
            btnFilter.Size = new Size(75, 23);
            btnFilter.TabIndex = 13;
            btnFilter.Text = "Lọc";
            btnFilter.UseVisualStyleBackColor = true;
            // 
            // lblFilterType
            // 
            lblFilterType.AutoSize = true;
            lblFilterType.Location = new Point(12, 70);
            lblFilterType.Name = "lblFilterType";
            lblFilterType.Size = new Size(32, 15);
            lblFilterType.TabIndex = 14;
            lblFilterType.Text = "Loại:";
            // 
            // cmbFilterType
            // 
            cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterType.FormattingEnabled = true;
            cmbFilterType.Location = new Point(48, 67);
            cmbFilterType.Name = "cmbFilterType";
            cmbFilterType.Size = new Size(121, 23);
            cmbFilterType.TabIndex = 15;
            // 
            // lblFilterAccessibility
            // 
            lblFilterAccessibility.AutoSize = true;
            lblFilterAccessibility.Location = new Point(175, 70);
            lblFilterAccessibility.Name = "lblFilterAccessibility";
            lblFilterAccessibility.Size = new Size(80, 15);
            lblFilterAccessibility.TabIndex = 16;
            lblFilterAccessibility.Text = "Mức truy cập:";
            // 
            // cmbFilterAccessibility
            // 
            cmbFilterAccessibility.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterAccessibility.FormattingEnabled = true;
            cmbFilterAccessibility.Location = new Point(255, 67);
            cmbFilterAccessibility.Name = "cmbFilterAccessibility";
            cmbFilterAccessibility.Size = new Size(121, 23);
            cmbFilterAccessibility.TabIndex = 17;
            // 
            // lblFilterFile
            // 
            lblFilterFile.AutoSize = true;
            lblFilterFile.Location = new Point(382, 70);
            lblFilterFile.Name = "lblFilterFile";
            lblFilterFile.Size = new Size(29, 15);
            lblFilterFile.TabIndex = 18;
            lblFilterFile.Text = "Tệp:";
            // 
            // cmbFilterFile
            // 
            cmbFilterFile.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterFile.FormattingEnabled = true;
            cmbFilterFile.Location = new Point(415, 67);
            cmbFilterFile.Name = "cmbFilterFile";
            cmbFilterFile.Size = new Size(177, 23);
            cmbFilterFile.TabIndex = 19;
            // 
            // lblFilterClass
            // 
            lblFilterClass.AutoSize = true;
            lblFilterClass.Location = new Point(12, 99);
            lblFilterClass.Name = "lblFilterClass";
            lblFilterClass.Size = new Size(30, 15);
            lblFilterClass.TabIndex = 20;
            lblFilterClass.Text = "Lớp:";
            // 
            // cmbFilterClass
            // 
            cmbFilterClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilterClass.FormattingEnabled = true;
            cmbFilterClass.Location = new Point(48, 96);
            cmbFilterClass.Name = "cmbFilterClass";
            cmbFilterClass.Size = new Size(200, 23);
            cmbFilterClass.TabIndex = 21;
            // 
            // chkOnlyMethods
            // 
            chkOnlyMethods.AutoSize = true;
            chkOnlyMethods.Location = new Point(255, 98);
            chkOnlyMethods.Name = "chkOnlyMethods";
            chkOnlyMethods.Size = new Size(158, 19);
            chkOnlyMethods.TabIndex = 22;
            chkOnlyMethods.Text = "Chỉ tìm các phương thức";
            chkOnlyMethods.UseVisualStyleBackColor = true;
            // 
            // btnExportExcel
            // 
            btnExportExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnExportExcel.Location = new Point(325, 454);
            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Size = new Size(100, 23);
            btnExportExcel.TabIndex = 28;
            btnExportExcel.Text = "Xuất Excel";
            btnExportExcel.UseVisualStyleBackColor = true;
            // 
            // btnImportExcel
            // 
            btnImportExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnImportExcel.Location = new Point(431, 454);
            btnImportExcel.Name = "btnImportExcel";
            btnImportExcel.Size = new Size(100, 23);
            btnImportExcel.TabIndex = 29;
            btnImportExcel.Text = "Nhập Excel";
            btnImportExcel.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(697, 451);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(75, 23);
            btnClose.TabIndex = 30;
            btnClose.Text = "Đóng";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click_1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 486);
            Controls.Add(btnClose);
            Controls.Add(btnImportExcel);
            Controls.Add(btnExportExcel);
            Controls.Add(chkOnlyMethods);
            Controls.Add(cmbFilterClass);
            Controls.Add(lblFilterClass);
            Controls.Add(cmbFilterFile);
            Controls.Add(lblFilterFile);
            Controls.Add(cmbFilterAccessibility);
            Controls.Add(lblFilterAccessibility);
            Controls.Add(cmbFilterType);
            Controls.Add(lblFilterType);
            Controls.Add(btnFilter);
            Controls.Add(txtFilter);
            Controls.Add(lblFilter);
            Controls.Add(btnCopySelected);
            Controls.Add(btnRefresh);
            Controls.Add(btnDeleteSelected);
            Controls.Add(txtResults);
            Controls.Add(lblResults);
            Controls.Add(dgvUnusedMembers);
            Controls.Add(btnScan);
            Controls.Add(btnSelectFolder);
            Controls.Add(txtFolderPath);
            Controls.Add(lblFolderPath);
            MinimumSize = new Size(600, 400);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Remove Unused Members Tool";
            ((System.ComponentModel.ISupportInitialize)dgvUnusedMembers).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblFolderPath;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.DataGridView dgvUnusedMembers;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.TextBox txtResults;
        private System.Windows.Forms.Button btnDeleteSelected;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnCopySelected;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Button btnFilter;
        private DataGridViewCheckBoxColumn colSelect;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colAccessibility;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colFilePath;
        private DataGridViewTextBoxColumn colLineNumber;
        private DataGridViewTextBoxColumn colContainingType;
        private Label lblFilterType;
        private ComboBox cmbFilterType;
        private Label lblFilterAccessibility;
        private ComboBox cmbFilterAccessibility;
        private Label lblFilterFile;
        private ComboBox cmbFilterFile;
        private Label lblFilterClass;
        private ComboBox cmbFilterClass;
        private CheckBox chkOnlyMethods;
        private Button btnExportExcel;
        private Button btnImportExcel;
        private Button btnClose;
    }
}