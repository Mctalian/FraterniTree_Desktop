using System;
using System.Windows.Forms;

namespace FraterniTree
{

    public partial class EditBrotherNoBig : Form
    {
        private readonly Brother brotherUnderEdit;

        public EditBrotherNoBig(Brother brother)
        {
            InitializeComponent();
            brotherUnderEdit = brother;
            lblEditBig.Text = string.Format(Util.GetLocalizedString("PromptUserForFullName"), brother);
            tbEditBig.AutoCompleteCustomSource = FrmMain.CurrentBrothers;
            btnCancelEdit.Enabled = true;
        }

        private void tbEditBig_TextChanged(object sender, EventArgs eventArgs)
        {
            if( tbEditBig.Text == string.Empty )
            {
                btnOK.Enabled = false;
                return;
            }
            btnOK.Enabled = true;
        }

        private void btnCancelEdit_Click(object sender, EventArgs eventArgs)
        {
            Close();
        }

        private void btnOK_Click(object sender, EventArgs eventArgs)
        {
            var b = FrmMain.Root.FindBrotherByName( tbEditBig.Text );
            if( b == null )
            {
                var space = tbEditBig.Text.IndexOf(' ');
                b = new Brother(tbEditBig.Text.Substring(space + 1), tbEditBig.Text.Substring(0, space), Util.DefaultInitiationTerm, Util.DefaultYear);
                FrmMain.Root.AddChild( b );
            }
            b.AddChild( brotherUnderEdit );
        }
    }

}