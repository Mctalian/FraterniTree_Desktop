using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FraterniTree
{
    public partial class EditBrotherNoBig : Form
    {
        private const string PRETEXT = "Please enter the full name of ";
        private const string POSTTEXT = "\'s Big:";
        public Brother m_BrotherUnderEdit;
        public EditBrotherNoBig(Brother b)
        {
            InitializeComponent();
            m_BrotherUnderEdit = b;
            lblEditBig.Text = PRETEXT + b.GetFullName() + POSTTEXT;
            tbEditBig.AutoCompleteCustomSource = frmMain.CurrentBrothers;
            btnCancelEdit.Enabled = true;
        }

        private void tbEditBig_TextChanged(object sender, EventArgs e)
        {
            if (tbEditBig.Text == "")
            {
                btnOK.Enabled = false;
                return;
            }
            btnOK.Enabled = true;
        }

        private void btnCancelEdit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Brother b = frmMain.root.FindBrotherByName(tbEditBig.Text);
            if (b == null)
            {
                int space = tbEditBig.Text.IndexOf(' ');
                b = new Brother(tbEditBig.Text.Substring(space + 1), tbEditBig.Text.Substring(0, space), "Fall", 1920);
                frmMain.root.AddChild(b);
            }
            b.AddChild(m_BrotherUnderEdit);
        }
    }
}
