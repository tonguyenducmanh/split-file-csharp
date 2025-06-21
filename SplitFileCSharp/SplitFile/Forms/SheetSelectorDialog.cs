using System.ComponentModel;

namespace SplitFile.Forms
{
    public class SheetSelectorDialog : Form
    {
        private ComboBox cboSheets;
        private Button btnOK;
        private Button btnCancel;
        private Label lblMessage;

        public string SelectedSheet => cboSheets.SelectedItem?.ToString() ?? "";

        public SheetSelectorDialog(List<string> sheets)
        {
            InitializeComponent();
            cboSheets.Items.AddRange(sheets.ToArray());
            if (cboSheets.Items.Count > 0)
                cboSheets.SelectedIndex = 0;
        }

        private void InitializeComponent()
        {
            this.cboSheets = new ComboBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblMessage = new Label();
            this.SuspendLayout();
            // 
            // cboSheets
            // 
            this.cboSheets.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboSheets.FormattingEnabled = true;
            this.cboSheets.Location = new Point(12, 37);
            this.cboSheets.Name = "cboSheets";
            this.cboSheets.Size = new Size(360, 23);
            this.cboSheets.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Location = new Point(216, 76);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "Chọn";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(297, 76);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Đóng";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Location = new Point(12, 9);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new Size(167, 15);
            this.lblMessage.TabIndex = 3;
            this.lblMessage.Text = "Chọn sheet cần nhập dữ liệu:";
            // 
            // SheetSelectorDialog
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(384, 111);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cboSheets);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SheetSelectorDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Chọn Sheet";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}