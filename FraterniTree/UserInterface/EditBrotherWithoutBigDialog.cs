using System;
using System.Windows.Forms;

namespace FraterniTree.UserInterface
{

    public partial class EditBrotherWithoutBigDialog : Form
    {
        private readonly Brother brotherUnderEdit;

        public EditBrotherWithoutBigDialog(Brother brother)
        {
            EditBrotherWithoutBig_Initialize();
            brotherUnderEdit = brother;
            lblEditBig.Text = string.Format(Util.GetLocalizedString("PromptUserForFullName"), brother);
            tbEditBig.AutoCompleteCustomSource = FamilyTreeForm.CurrentBrothers;
            btnCancelEdit.Enabled = true;
        }

        private void EditBrotherWithoutBig_Name_onChange(object sender, EventArgs eventArgs)
        {
            if( tbEditBig.Text == string.Empty )
            {
                btnOK.Enabled = false;
                return;
            }
            btnOK.Enabled = true;
        }

        private void EditBrotherWithoutBig_Cancel_onClick(object sender, EventArgs eventArgs)
        {
            Close();
        }

        private void EditBrotherWithoutBig_Ok_onClick(object sender, EventArgs eventArgs)
        {
            var b = FamilyTreeForm.Root.FindDescendant( tbEditBig.Text );
            if( b == null )
            {
                var space = tbEditBig.Text.IndexOf(' ');
                b = new Brother(tbEditBig.Text.Substring(space + 1), tbEditBig.Text.Substring(0, space), Util.DefaultInitiationTerm, Util.DefaultYear);
                FamilyTreeForm.Root.AddChild( b );
            }
            b.AddChild( brotherUnderEdit );
        }
    
    }

}