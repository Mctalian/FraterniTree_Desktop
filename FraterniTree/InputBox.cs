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
    public partial class InputBox : Form
    {

        public string UserResponse { get; set; }

        public InputBox(string Prompt)
        {
            InitializeComponent();
            lblPrompt.Text = Prompt;
            btnCancelEdit.Enabled = true;
        }

        public InputBox(string Prompt, string Title)
        {
            InitializeComponent();
            lblPrompt.Text = Prompt;
            this.Text = Title;
            btnCancelEdit.Enabled = true;
        }

        public InputBox(string Prompt, string Title, AutoCompleteStringCollection ACSC)
        {
            InitializeComponent();
            lblPrompt.Text = Prompt;
            this.Text = Title;
            tbInput.AutoCompleteCustomSource = ACSC;
            btnCancelEdit.Enabled = true;
        }

        private void tbInput_TextChanged(object sender, EventArgs e)
        {
            if (tbInput.Text == "")
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
            UserResponse = tbInput.Text;
        }
    }
}
