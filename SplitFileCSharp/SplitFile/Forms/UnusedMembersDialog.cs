using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SplitFile.Forms
{
    public partial class UnusedMembersDialog : Form
    {
        public List<MemberDeclarationSyntax> SelectedMembers { get; private set; }
        private readonly List<MemberDeclarationSyntax> _members;

        public UnusedMembersDialog(List<MemberDeclarationSyntax> unusedMembers)
        {
            _members = unusedMembers;
            SelectedMembers = new List<MemberDeclarationSyntax>();
            InitializeComponent();
            LoadMembers();
        }

        private void LoadMembers()
        {
            foreach (var member in _members)
            {
                var item = listUnused.Items.Add(GetMemberName(member));
                item.SubItems.Add(member.Modifiers.ToString());
                item.Checked = true;
            }
        }

        private string GetMemberName(MemberDeclarationSyntax member)
        {
            return member switch
            {
                MethodDeclarationSyntax method => $"Method: {method.Identifier.Text}",
                PropertyDeclarationSyntax property => $"Property: {property.Identifier.Text}",
                FieldDeclarationSyntax field => $"Field: {field.Declaration.Variables.First().Identifier.Text}",
                _ => member.ToString()
            };
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedMembers = new List<MemberDeclarationSyntax>();
            for (int i = 0; i < listUnused.Items.Count; i++)
            {
                if (listUnused.Items[i].Checked)
                {
                    SelectedMembers.Add(_members[i]);
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listUnused.Items)
            {
                item.Checked = true;
            }
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listUnused.Items)
            {
                item.Checked = false;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}