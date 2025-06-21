namespace SplitFile.Forms
{
    partial class UnusedMembersDialog
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
            listUnused = new ListView();
            colName = new ColumnHeader();
            colModifiers = new ColumnHeader();
            lblTitle = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            btnSelectAll = new Button();
            btnDeselectAll = new Button();
            SuspendLayout();
            // 
            // listUnused
            // 
            listUnused.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listUnused.CheckBoxes = true;
            listUnused.Columns.AddRange(new ColumnHeader[] { colName, colModifiers });
            listUnused.FullRowSelect = true;
            listUnused.Location = new Point(12, 38);
            listUnused.Name = "listUnused";
            listUnused.Size = new Size(560, 371);
            listUnused.TabIndex = 0;
            listUnused.UseCompatibleStateImageBehavior = false;
            listUnused.View = View.Details;
            listUnused.GridLines = true;
            // 
            // colName
            // 
            colName.Text = "Tên";
            colName.Width = 300;
            // 
            // colModifiers
            // 
            colModifiers.Text = "Modifiers";
            colModifiers.Width = 200;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(12, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(200, 15);
            lblTitle.TabIndex = 1;
            lblTitle.Text = "Các thành phần không được sử dụng:";
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(416, 415);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 4;
            btnOK.Text = "Xóa";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(497, 415);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Đóng";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSelectAll
            // 
            btnSelectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSelectAll.Location = new Point(254, 415);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new Size(75, 23);
            btnSelectAll.TabIndex = 2;
            btnSelectAll.Text = "Chọn tất cả";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += btnSelectAll_Click;
            // 
            // btnDeselectAll
            // 
            btnDeselectAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDeselectAll.Location = new Point(335, 415);
            btnDeselectAll.Name = "btnDeselectAll";
            btnDeselectAll.Size = new Size(75, 23);
            btnDeselectAll.TabIndex = 3;
            btnDeselectAll.Text = "Bỏ chọn";
            btnDeselectAll.UseVisualStyleBackColor = true;
            btnDeselectAll.Click += btnDeselectAll_Click;
            // 
            // UnusedMembersDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(584, 450);
            Controls.Add(btnDeselectAll);
            Controls.Add(btnSelectAll);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(lblTitle);
            Controls.Add(listUnused);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UnusedMembersDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Xóa thành phần không sử dụng";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listUnused;
        private ColumnHeader colName;
        private ColumnHeader colModifiers;
        private Label lblTitle;
        private Button btnOK;
        private Button btnCancel;
        private Button btnSelectAll;
        private Button btnDeselectAll;
    }
}