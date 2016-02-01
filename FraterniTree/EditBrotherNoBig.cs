using System;
using System.Windows.Forms;

namespace FraterniTree
{
    public partial class EditBrotherNoBig : Form
    {

        private const string PRETEXT = "Please enter the full name of "; //TODO
        private const string POSTTEXT = "\'s Big:";
        public Brother BrotherUnderEdit;

        public EditBrotherNoBig(Brother b)
        {
            InitializeComponent();
            BrotherUnderEdit = b;
            lblEditBig.Text = PRETEXT + b + POSTTEXT;
            tbEditBig.AutoCompleteCustomSource = frmMain.CurrentBrothers;
            btnCancelEdit.Enabled = true;
        }

        private void tbEditBig_TextChanged(object sender, EventArgs e)
        {
            if( tbEditBig.Text == string.Empty )
            {
                btnOK.Enabled = false;
                return;
            }
            btnOK.Enabled = true;
        }

        private void btnCancelEdit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var b = frmMain.root.FindBrotherByName( tbEditBig.Text );
            if( b == null )
            {
                var space = tbEditBig.Text.IndexOf( ' ' );
                b = new Brother( tbEditBig.Text.Substring( space + 1 ), tbEditBig.Text.Substring( 0, space ), "Fall",
                    1920 );
                frmMain.root.AddChild( b );
            }
            b.AddChild( BrotherUnderEdit );
        }

    }
}